using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YelG.WebSite.Application;
using YelG.WebSite.Application.Abstractions;
using YelG.WebSite.Application.Content;
using YelG.WebSite.Infrastructure;
using YelG.WebSite.Infrastructure.Data;
using YelG.WebSite.Infrastructure.Identity;
using YelG.WebSite.Infrastructure.Seeding;

namespace YelG.WebSite.Tests
{
    [Collection(WebSiteDatabaseCollection.Name)]
    public class HomeContentTests
    {
        public HomeContentTests(WebSiteDatabase db) => _ = db;   // migrated by the shared fixture

        /// <summary>One application (shared cache and content version), like the running site.</summary>
        private static ServiceProvider Site()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICurrentUser>(new TestUser(null));
            services.AddWebSiteApplication();
            services.AddWebSiteInfrastructure(WebSiteDatabase.ConnectionString);
            services.AddIdentityCore<AdminUser>(IdentityPolicy.Configure).AddRoles<AdminRole>().AddEntityFrameworkStores<WebSiteDbContext>();
            return services.BuildServiceProvider();
        }

        [LocalDbFact]
        public async Task The_cached_page_is_rebuilt_after_a_content_save_and_hidden_items_show_only_in_preview()
        {
            await using var site = Site();
            await using (var scope = site.CreateAsyncScope())
                await scope.ServiceProvider.GetRequiredService<ContentSeeder>().SeedAsync(new SeedOptions { AdminEmail = "admin@test.local", AdminPassword = "seed-pass-123" });

            HomeContent first;
            await using (var scope = site.CreateAsyncScope())
            {
                var content = scope.ServiceProvider.GetRequiredService<HomeContentService>();
                first = await content.GetAsync();
                Assert.Same(first, await content.GetAsync());   // served from the cache
                Assert.Equal(9, first.ServiceList.Count);
                Assert.All(first.ServiceList, s => Assert.NotEmpty(s.Deliverables));
            }

            await using (var scope = site.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
                var step = await db.HowSteps.OrderBy(s => s.SortOrder).FirstAsync();
                step.IsPublished = false;
                await db.SaveChangesAsync();
            }

            await using (var scope = site.CreateAsyncScope())
            {
                var content = scope.ServiceProvider.GetRequiredService<HomeContentService>();
                var after = await content.GetAsync();
                Assert.NotSame(first, after);
                Assert.Equal(first.HowSteps.Count - 1, after.HowSteps.Count);
                Assert.Equal(first.HowSteps.Count, (await content.GetAsync(preview: true)).HowSteps.Count);
            }

            await using (var scope = site.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
                await db.HowSteps.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPublished, true));
            }
        }
    }
}
