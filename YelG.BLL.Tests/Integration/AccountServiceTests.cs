using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using YelG.BLL.Common;
using YelG.BLL.Repository;
using YelG.BLL.Repository.Audit;
using YelG.BLL.Services.Users;
using YelG.DAL.Data;
using YelG.Model.Auth;

namespace YelG.BLL.Tests.Integration
{
    [Collection(ServiceDatabaseCollection.Name)]
    public class AccountServiceTests
    {
        private const string Original = "start-pass1";

        private readonly ServiceDatabase _db;

        public AccountServiceTests(ServiceDatabase db) => _db = db;

        /// <summary>A real UserManager over the test database, with the app's password rules.</summary>
        internal static UserManager<User> Users(YelgDbContext context)
        {
            var options = new IdentityOptions();
            IdentityPolicy.Configure(options);
            var manager = new UserManager<User>(
                new UserStore<User, Role, YelgDbContext, long, UserClaim, UserRole, UserLogin, userToken, RoleClaim>(context), Options.Create(options), new PasswordHasher<User>(),
                new[] { new UserValidator<User>() }, new[] { new PasswordValidator<User>() },
                new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!,
                NullLogger<UserManager<User>>.Instance);
            // Password resets by an admin need a token provider (the app gets the default ones from Identity).
            manager.RegisterTokenProvider(TokenOptions.DefaultProvider, new TestTokenProvider());
            return manager;
        }

        private sealed class TestTokenProvider : IUserTwoFactorTokenProvider<User>
        {
            public Task<string> GenerateAsync(string purpose, UserManager<User> manager, User user) => Task.FromResult(purpose + ":" + user.Id);
            public Task<bool> ValidateAsync(string purpose, string token, UserManager<User> manager, User user) => Task.FromResult(token == purpose + ":" + user.Id);
            public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<User> manager, User user) => Task.FromResult(false);
        }

        /// <summary>A fresh account with a known password, so tests do not touch the shared reviewers.</summary>
        private async Task<long> NewUserAsync()
        {
            await using var context = _db.NewContext(new TestUser(null, false));
            var user = new User
            {
                UserName = "pw-" + Guid.NewGuid().ToString("N")[..10], DisplayName = "اختبار كلمة المرور",
                IsActive = true, MustChangePassword = true   // like an account an admin just created
            };
            IdentityErrors.ThrowIfFailed(await Users(context).CreateAsync(user, Original));
            return user.Id;
        }

        private sealed class Scope : IAsyncDisposable
        {
            public Scope(ServiceDatabase db, long userId, LoginThrottle throttle)
            {
                var user = new TestUser(userId, false);
                Context = db.NewContext(user);
                UserManager = Users(Context);
                Account = new AccountService(UserManager, new ActivityLogRepository(Context, user, TimeProvider.System),
                    new UnitOfWork(Context), user, throttle);
            }

            public YelgDbContext Context { get; }
            public UserManager<User> UserManager { get; }
            public IAccountService Account { get; }

            public ValueTask DisposeAsync() => Context.DisposeAsync();
        }

        private static LoginThrottle NewThrottle() => new(new LoginThrottleOptions(), TimeProvider.System);

        [LocalDbFact]
        public async Task Changes_the_password_and_the_security_stamp_and_logs_it()
        {
            var userId = await NewUserAsync();
            string stampBefore;
            await using (var before = new Scope(_db, userId, NewThrottle()))
                stampBefore = (await before.UserManager.FindByIdAsync(userId.ToString()))!.SecurityStamp!;

            await using (var scope = new Scope(_db, userId, NewThrottle()))
                await scope.Account.ChangePasswordAsync(Original, "new-pass22", "10.0.0.1");

            await using var check = new Scope(_db, userId, NewThrottle());
            var user = (await check.UserManager.FindByIdAsync(userId.ToString()))!;
            Assert.True(await check.UserManager.CheckPasswordAsync(user, "new-pass22"));
            Assert.False(await check.UserManager.CheckPasswordAsync(user, Original));
            Assert.NotEqual(stampBefore, user.SecurityStamp);   // other sessions end at the next stamp check
            Assert.False(user.MustChangePassword);              // the temporary-password hold is lifted
            Assert.True(await check.Context.ActivityLogs.AnyAsync(l => l.UserId == userId && l.Action == "PASSWORD_CHANGED"));
        }

        [LocalDbFact]
        public async Task Rejects_a_wrong_current_password_a_weak_or_unchanged_new_one_in_arabic()
        {
            var userId = await NewUserAsync();
            await using var scope = new Scope(_db, userId, NewThrottle());

            async Task<string> Rejected(string current, string next) =>
                (await Assert.ThrowsAsync<BusinessRuleException>(() => scope.Account.ChangePasswordAsync(current, next, "10.0.0.2"))).Message;

            Assert.Equal("كلمة المرور الحالية غير صحيحة.", await Rejected("wrong-pass1", "new-pass22"));
            Assert.Equal("كلمة المرور يجب ألا تقل عن 8 خانات.", await Rejected(Original, "a1"));
            Assert.Equal("كلمة المرور يجب أن تحتوي على رقم.", await Rejected(Original, "no-digits-here"));
            Assert.Equal("كلمة المرور الجديدة يجب أن تختلف عن الحالية.", await Rejected(Original, Original));

            Assert.True(await scope.Context.ActivityLogs.AnyAsync(l => l.UserId == userId && l.Action == "PASSWORD_CHANGE_FAILED"));
            var user = (await scope.UserManager.FindByIdAsync(userId.ToString()))!;
            Assert.True(await scope.UserManager.CheckPasswordAsync(user, Original));   // nothing changed
        }

        [LocalDbFact]
        public async Task Repeated_wrong_current_passwords_are_throttled_like_sign_in()
        {
            var userId = await NewUserAsync();
            var throttle = NewThrottle();   // shared, like the singleton in the app
            await using var scope = new Scope(_db, userId, throttle);

            for (var i = 0; i < new LoginThrottleOptions().MaxFailuresPerUser; i++)
                await Assert.ThrowsAsync<BusinessRuleException>(() => scope.Account.ChangePasswordAsync("wrong-pass1", "new-pass22", "10.0.0.3"));

            var blocked = await Assert.ThrowsAsync<BusinessRuleException>(() => scope.Account.ChangePasswordAsync(Original, "new-pass22", "10.0.0.3"));
            Assert.StartsWith("محاولات خاطئة كثيرة.", blocked.Message);
            Assert.True(await scope.Context.ActivityLogs.AnyAsync(l => l.UserId == userId && l.Action == "LOGIN_THROTTLED"));
        }
    }
}
