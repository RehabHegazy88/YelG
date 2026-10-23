using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YelG.DAL.Data;
using YelG.Model.Auth;
using YelG.Model.Scope;

namespace YelG.DAL.Seeding
{
    /// <summary>
    /// Replaces setupYELG(): applies migrations, creates roles, the pricing settings row and
    /// the first Admin. The Admin password comes from configuration (user-secrets), never from code.
    /// </summary>
    public sealed class DbSeeder
    {
        private readonly YelgDbContext _db;
        private readonly UserManager<User> _users;
        private readonly RoleManager<Role> _roles;
        private readonly TimeProvider _clock;
        private readonly ILogger<DbSeeder> _logger;

        public DbSeeder(
            YelgDbContext db,
            UserManager<User> users,
            RoleManager<Role> roles,
            TimeProvider clock,
            ILogger<DbSeeder> logger)
        {
            _db = db;
            _users = users;
            _roles = roles;
            _clock = clock;
            _logger = logger;
        }

        public async Task SeedAsync(string? adminUserName, string? adminPassword, CancellationToken ct = default)
        {
            await _db.Database.MigrateAsync(ct);

            foreach (var role in new[] { AppRoles.Admin, AppRoles.User })
            {
                if (!await _roles.RoleExistsAsync(role))
                    await _roles.CreateAsync(new Role { Name = role, Type = AppRoles.TypeOf(role) });
            }

            if (!await _db.PricingSettings.AnyAsync(ct))
            {
                _db.PricingSettings.Add(new PricingSettings
                {
                    MethodologyNote = "الساعات افتراضات مبدئية وتحتاج معايرة من واقع ساعات YELG الفعلية."
                });
                await _db.SaveChangesAsync(ct);
            }

            await SeedQuestionBankAsync(ct);
            await SeedTaskLibraryAsync(ct);

            if ((await _users.GetUsersInRoleAsync(AppRoles.Admin)).Count > 0) return;

            if (string.IsNullOrWhiteSpace(adminUserName) || string.IsNullOrWhiteSpace(adminPassword))
            {
                _logger.LogWarning("No Admin account exists. Set Seed:AdminUserName and Seed:AdminPassword (user-secrets) and restart.");
                return;
            }

            var admin = new User
            {
                UserName = adminUserName,
                DisplayName = "YELG Admin",
                IsActive = true,
                // The password comes from configuration, so others may know it: replaced at first sign-in.
                MustChangePassword = true
            };
            var created = await _users.CreateAsync(admin, adminPassword);
            if (!created.Succeeded)
            {
                _logger.LogError("Could not create the Admin account: {Errors}",
                    string.Join("; ", created.Errors.Select(e => e.Description)));
                return;
            }
            await _users.AddToRoleAsync(admin, AppRoles.Admin);
            _logger.LogInformation("Admin account '{UserName}' created.", adminUserName);
        }

        /// <summary>First run only: version 1 of the question bank, published.</summary>
        private async Task SeedQuestionBankAsync(CancellationToken ct)
        {
            if (await _db.QuestionBankVersions.AnyAsync(ct)) return;

            var version = SeedData.LoadQuestionBank();
            version.PublishedAt = _clock.GetUtcNow();
            _db.QuestionBankVersions.Add(version);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Seeded question bank v{Version}: {Categories} categories, {Questions} questions.",
                version.VersionNo, version.Categories.Count, version.Categories.Sum(c => c.Questions.Count));
        }

        /// <summary>First run only: the YELG task library and answer rules imported from Excel.</summary>
        private async Task SeedTaskLibraryAsync(CancellationToken ct)
        {
            if (await _db.TaskTemplates.AnyAsync(ct)) return;

            var (templates, rules) = SeedData.LoadTaskLibrary();
            _db.TaskTemplates.AddRange(templates);
            _db.AnswerRules.AddRange(rules);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Seeded {Templates} task templates and {Rules} answer rules.", templates.Count, rules.Count);
        }
    }
}
