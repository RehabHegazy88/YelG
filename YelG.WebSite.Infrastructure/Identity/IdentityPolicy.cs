using Microsoft.AspNetCore.Identity;

namespace YelG.WebSite.Infrastructure.Identity
{
    /// <summary>Password, lockout and user rules for the admin panel, shared by the web app and the tests.</summary>
    public static class IdentityPolicy
    {
        public static void Configure(IdentityOptions options)
        {
            options.Password.RequiredLength = 10;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.User.RequireUniqueEmail = true;
        }
    }
}
