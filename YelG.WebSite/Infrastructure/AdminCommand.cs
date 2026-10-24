using Microsoft.AspNetCore.Identity;
using YelG.WebSite.Application.Common;
using YelG.WebSite.Infrastructure.Identity;

namespace YelG.WebSite.Infrastructure
{
    /// <summary>
    /// "--create-admin email [display name]": creates an Admin, or resets an existing account to an active
    /// Admin, with the password from the YELG_ADMIN_PASSWORD environment variable (never a command-line
    /// argument, which would stay in the shell history). The password must be changed at the next sign-in.
    /// For the server operator when no Admin can sign in.
    /// </summary>
    public static class AdminCommand
    {
        public const string Flag = "--create-admin";
        public const string PasswordVariable = "YELG_ADMIN_PASSWORD";

        public static async Task<int> RunAsync(IServiceProvider services, string[] args)
        {
            var at = Array.IndexOf(args, Flag);
            var email = at + 1 < args.Length ? args[at + 1].Trim() : "";
            var name = at + 2 < args.Length && !args[at + 2].StartsWith("--", StringComparison.Ordinal) ? args[at + 2].Trim() : "YelG Admin";
            var password = Environment.GetEnvironmentVariable(PasswordVariable);
            if (email.Length == 0 || string.IsNullOrEmpty(password))
            {
                Console.Error.WriteLine($"Usage: {Flag} <email> [\"Display name\"], with the password in the {PasswordVariable} environment variable.");
                return 2;
            }

            using var scope = services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AdminUser>>();
            var user = await users.FindByEmailAsync(email);
            IdentityResult result;
            if (user is null)
            {
                user = new AdminUser
                {
                    UserName = email, Email = email, EmailConfirmed = true, DisplayName = name,
                    IsActive = true, MustChangePassword = true, LockoutEnabled = true
                };
                result = await users.CreateAsync(user, password);
            }
            else
            {
                user.IsActive = true;
                user.MustChangePassword = true;
                result = await users.UpdateAsync(user);
                if (result.Succeeded) result = await users.RemovePasswordAsync(user);
                if (result.Succeeded) result = await users.AddPasswordAsync(user, password);
                if (result.Succeeded) await users.SetLockoutEndDateAsync(user, null);
            }
            if (result.Succeeded && !await users.IsInRoleAsync(user, AdminRoles.Admin))
            {
                var others = (await users.GetRolesAsync(user)).ToList();
                if (others.Count > 0) await users.RemoveFromRolesAsync(user, others);
                result = await users.AddToRoleAsync(user, AdminRoles.Admin);
            }

            if (!result.Succeeded)
            {
                Console.Error.WriteLine("Failed: " + string.Join("; ", result.Errors.Select(e => e.Description)));
                return 1;
            }
            Console.WriteLine($"Admin '{email}' is ready; the password must be changed at the next sign-in.");
            return 0;
        }
    }
}
