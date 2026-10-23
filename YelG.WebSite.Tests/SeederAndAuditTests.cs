using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YelG.WebSite.Application.Abstractions;
using YelG.WebSite.Domain.Sections;
using YelG.WebSite.Infrastructure;
using YelG.WebSite.Infrastructure.Data;
using YelG.WebSite.Infrastructure.Identity;
using YelG.WebSite.Infrastructure.Seeding;

namespace YelG.WebSite.Tests
{
    /// <summary>Runs on Windows with LocalDB; set YELG_SKIP_LOCALDB to skip.</summary>
    public sealed class LocalDbFactAttribute : FactAttribute
    {
        public LocalDbFactAttribute()
        {
            if (!OperatingSystem.IsWindows() || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("YELG_SKIP_LOCALDB")))
                Skip = "Needs SQL Server LocalDB.";
        }
    }

    public sealed record TestUser(long? UserId, string? UserName = null, bool IsAdmin = false) : ICurrentUser;

    /// <summary>A throw-away database (YelgWebSite_Tests on LocalDB), migrated once and dropped at the end.</summary>
    public sealed class WebSiteDatabase : IAsyncLifetime
    {
        public const string ConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=YelgWebSite_Tests;Trusted_Connection=True;TrustServerCertificate=True";

        /// <summary>A service scope acting as the given user (null id = the seeder / a public visitor).</summary>
        public AsyncServiceScope Scope(TestUser user)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICurrentUser>(user);
            services.AddWebSiteInfrastructure(ConnectionString);
            services.AddIdentityCore<AdminUser>(IdentityPolicy.Configure)
                .AddRoles<AdminRole>()
                .AddEntityFrameworkStores<WebSiteDbContext>();
            return services.BuildServiceProvider().CreateAsyncScope();
        }

        public async Task InitializeAsync()
        {
            if (!OperatingSystem.IsWindows() || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("YELG_SKIP_LOCALDB"))) return;
            await using var scope = Scope(new TestUser(null));
            var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            if (!OperatingSystem.IsWindows() || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("YELG_SKIP_LOCALDB"))) return;
            await using var scope = Scope(new TestUser(null));
            await scope.ServiceProvider.GetRequiredService<WebSiteDbContext>().Database.EnsureDeletedAsync();
        }
    }

    [CollectionDefinition(Name)]
    public sealed class WebSiteDatabaseCollection : ICollectionFixture<WebSiteDatabase>
    {
        public const string Name = "Website database";
    }

    [Collection(WebSiteDatabaseCollection.Name)]
    public class SeederAndAuditTests
    {
        private readonly WebSiteDatabase _db;

        public SeederAndAuditTests(WebSiteDatabase db) => _db = db;

        private static readonly SeedOptions Options = new() { AdminEmail = "admin@test.local", AdminPassword = "seed-pass-123" };

        private async Task<IReadOnlyDictionary<string, int>> SeedAsync()
        {
            await using var scope = _db.Scope(new TestUser(null));
            var seeder = scope.ServiceProvider.GetRequiredService<ContentSeeder>();
            await seeder.SeedAsync(Options);
            return seeder.Added;
        }

        private static async Task<Dictionary<string, int>> CountsAsync(WebSiteDbContext db) => new()
        {
            ["Services"] = await db.Services.IgnoreQueryFilters().CountAsync(),
            ["ServiceDeliverables"] = await db.ServiceDeliverables.IgnoreQueryFilters().CountAsync(),
            ["FaqItems"] = await db.FaqItems.IgnoreQueryFilters().CountAsync(),
            ["SelfCheckQuestions"] = await db.SelfCheckQuestions.IgnoreQueryFilters().CountAsync(),
            ["HeroSections"] = await db.HeroSections.IgnoreQueryFilters().CountAsync(),
            ["MediaAssets"] = await db.MediaAssets.IgnoreQueryFilters().CountAsync(),
            ["Users"] = await db.Users.CountAsync()
        };

        [LocalDbFact]
        public async Task Seeding_again_adds_nothing_keeps_edits_and_does_not_restore_deleted_items()
        {
            await SeedAsync();
            Dictionary<string, int> before;
            long editedId, deletedId;
            await using (var scope = _db.Scope(new TestUser(null)))
            {
                var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
                before = await CountsAsync(db);
                Assert.Equal(9, before["Services"]);
                Assert.Equal(58, before["ServiceDeliverables"]);
                Assert.Equal(1, before["HeroSections"]);
                Assert.Equal(1, before["Users"]);

                var faq = await db.FaqItems.OrderBy(f => f.SortOrder).ToListAsync();
                faq[0].Question.Ar = "سؤال عدّله المحرر";
                editedId = faq[0].Id;
                db.FaqItems.Remove(faq[1]);   // soft delete
                deletedId = faq[1].Id;
                await db.SaveChangesAsync();
            }

            var added = await SeedAsync();
            Assert.Empty(added);   // nothing new

            await using (var scope = _db.Scope(new TestUser(null)))
            {
                var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
                Assert.Equal(before, await CountsAsync(db));
                Assert.Equal("سؤال عدّله المحرر", (await db.FaqItems.SingleAsync(f => f.Id == editedId)).Question.Ar);
                Assert.False(await db.FaqItems.AnyAsync(f => f.Id == deletedId));   // hidden by the filter
                Assert.True((await db.FaqItems.IgnoreQueryFilters().SingleAsync(f => f.Id == deletedId)).Deleted);
            }
        }

        [LocalDbFact]
        public async Task An_edit_by_a_signed_in_user_is_stamped_and_audited_with_old_and_new_values()
        {
            await SeedAsync();
            long userId;
            await using (var scope = _db.Scope(new TestUser(null)))
                userId = (await scope.ServiceProvider.GetRequiredService<WebSiteDbContext>().Users.FirstAsync()).Id;

            long stepId;
            string oldTitle;
            await using (var scope = _db.Scope(new TestUser(userId, "admin@test.local", true)))
            {
                var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
                var step = await db.HowSteps.OrderBy(s => s.SortOrder).FirstAsync();
                stepId = step.Id;
                oldTitle = step.Title.En;
                step.Title.En = "Discovery";
                await db.SaveChangesAsync();

                step.IsPublished = false;
                await db.SaveChangesAsync();
            }

            await using (var scope = _db.Scope(new TestUser(null)))
            {
                var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
                var step = await db.HowSteps.IgnoreQueryFilters().SingleAsync(s => s.Id == stepId);
                Assert.Equal(userId, step.ModifiedBy_PersonID);
                Assert.True(step.LastModifiedDate > step.AddedDate);

                var logs = await db.AuditLogs.Where(a => a.EntityType == nameof(HowStep) && a.EntityId == stepId.ToString())
                    .OrderBy(a => a.Id).ToListAsync();
                Assert.Equal(new[] { "Updated", "Hidden" }, logs.Select(l => l.Action));
                Assert.All(logs, l => Assert.Equal(userId, l.UserId));
                Assert.Contains("Title.En", logs[0].Changes);
                Assert.Contains(oldTitle, logs[0].Changes);
                Assert.Contains("Discovery", logs[0].Changes);
            }
        }
    }
}
