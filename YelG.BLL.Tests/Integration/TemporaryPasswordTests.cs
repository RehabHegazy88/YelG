using Microsoft.EntityFrameworkCore;
using YelG.BLL.Common;
using YelG.BLL.Repository;
using YelG.BLL.Repository.Audit;
using YelG.BLL.Repository.Users;
using YelG.BLL.Services.Users;
using YelG.BLL.ViewModel;
using YelG.Model.Auth;

namespace YelG.BLL.Tests.Integration
{
    /// <summary>A password an admin sets is temporary (User.MustChangePassword) until its holder replaces it.</summary>
    [Collection(ServiceDatabaseCollection.Name)]
    public class TemporaryPasswordTests
    {
        private const string Temporary = "temp-pass1";

        private readonly ServiceDatabase _db;

        public TemporaryPasswordTests(ServiceDatabase db) => _db = db;

        private UserAdminService AdminService(YelG.DAL.Data.YelgDbContext context, TestUser admin) =>
            new(AccountServiceTests.Users(context), new UserRepository(context),
                new ActivityLogRepository(context, admin, TimeProvider.System), new UnitOfWork(context), admin, TimeProvider.System);

        private static AccountService Account(YelG.DAL.Data.YelgDbContext context, TestUser user) =>
            new(AccountServiceTests.Users(context), new ActivityLogRepository(context, user, TimeProvider.System),
                new UnitOfWork(context), user, new LoginThrottle(new LoginThrottleOptions(), TimeProvider.System));

        /// <summary>The fixture seeds data, not Identity roles; the admin service needs them to assign one.</summary>
        private async Task EnsureRolesAsync()
        {
            await using var context = _db.NewContext(new TestUser(null, false));
            foreach (var name in new[] { AppRoles.Admin, AppRoles.User })
                if (!await context.Roles.AnyAsync(r => r.Name == name))
                    context.Roles.Add(new Role { Name = name, NormalizedName = name.ToUpperInvariant(), Type = AppRoles.TypeOf(name) });
            await context.SaveChangesAsync();
        }

        [LocalDbFact]
        public async Task An_account_created_by_an_admin_must_change_its_password_and_the_hold_lifts_only_on_success()
        {
            await EnsureRolesAsync();
            var admin = new TestUser(_db.AdminId, true);
            long userId;
            await using (var context = _db.NewContext(admin))
            {
                var service = AdminService(context, admin);
                userId = await service.CreateAsync(new SaveUserRequest("temp-" + Guid.NewGuid().ToString("N")[..8], "مستخدم مؤقت", false, true, Temporary));
                Assert.True((await service.ListAsync()).Single(u => u.Id == userId).MustChangePassword);   // shown on the users screen
            }

            var holder = new TestUser(userId, false);
            await using (var context = _db.NewContext(holder))
            {
                // A new password that breaks the rules is rejected and the hold stays.
                await Assert.ThrowsAsync<BusinessRuleException>(() => Account(context, holder).ChangePasswordAsync(Temporary, "short", null));
                await context.SaveChangesAsync();   // even a later save in the same request keeps the flag
            }
            await using (var check = _db.NewContext(holder))
                Assert.True((await check.Users.SingleAsync(u => u.Id == userId)).MustChangePassword);

            await using (var context = _db.NewContext(holder))
                await Account(context, holder).ChangePasswordAsync(Temporary, "chosen-pass22", null);
            await using (var check = _db.NewContext(holder))
                Assert.False((await check.Users.SingleAsync(u => u.Id == userId)).MustChangePassword);

            // A password the admin resets later is temporary again.
            await using (var context = _db.NewContext(admin))
                await AdminService(context, admin).UpdateAsync(userId, new SaveUserRequest("ignored", "مستخدم مؤقت", false, true, "reset-pass3"));
            await using (var check = _db.NewContext(holder))
                Assert.True((await check.Users.SingleAsync(u => u.Id == userId)).MustChangePassword);
        }
    }
}
