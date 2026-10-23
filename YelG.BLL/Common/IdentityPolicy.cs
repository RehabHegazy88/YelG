using Microsoft.AspNetCore.Identity;

namespace YelG.BLL.Common
{
    /// <summary>Password and lockout rules, shared by the web app and the tests so they cannot drift apart.</summary>
    public static class IdentityPolicy
    {
        public static void Configure(IdentityOptions options)
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.User.RequireUniqueEmail = false;
        }
    }
}
