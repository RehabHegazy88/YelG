using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using YelG.Model.Auth;

namespace YelG.AdminPortal.Infrastructure
{
    /// <summary>
    /// Adds the display name to the sign-in cookie so the layout can show it without a query, and marks
    /// accounts that still have a temporary password (User.MustChangePassword).
    /// </summary>
    public sealed class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, Role>
    {
        public const string DisplayNameClaim = "display_name";
        public const string MustChangePasswordClaim = "must_change_password";

        public AppClaimsPrincipalFactory(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IOptions<IdentityOptions> options)
            : base(userManager, roleManager, options)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            identity.AddClaim(new Claim(DisplayNameClaim, string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? "" : user.DisplayName));
            if (user.MustChangePassword) identity.AddClaim(new Claim(MustChangePasswordClaim, "true"));
            return identity;
        }
    }

    public static class ClaimsPrincipalExtensions
    {
        public static string DisplayName(this ClaimsPrincipal user) =>
            user.FindFirstValue(AppClaimsPrincipalFactory.DisplayNameClaim) ?? user.Identity?.Name ?? string.Empty;

        public static long? UserId(this ClaimsPrincipal user) =>
            long.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        public static bool MustChangePassword(this ClaimsPrincipal user) =>
            user.HasClaim(AppClaimsPrincipalFactory.MustChangePasswordClaim, "true");

        public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(AppRoles.Admin);
    }
}
