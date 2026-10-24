using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace YelG.WebSite.Infrastructure.Identity
{
    /// <summary>Claims the admin panel reads from the sign-in cookie, so it does not load the user on every request.</summary>
    public static class AdminClaims
    {
        public const string DisplayName = "yelg:display_name";

        /// <summary>Present while the account still has its temporary password.</summary>
        public const string MustChangePassword = "yelg:must_change_password";
    }

    public sealed class AdminClaimsPrincipalFactory : UserClaimsPrincipalFactory<AdminUser, AdminRole>
    {
        public AdminClaimsPrincipalFactory(UserManager<AdminUser> userManager, RoleManager<AdminRole> roleManager,
            IOptions<IdentityOptions> options) : base(userManager, roleManager, options)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AdminUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            identity.AddClaim(new Claim(AdminClaims.DisplayName, string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? "" : user.DisplayName));
            if (user.MustChangePassword) identity.AddClaim(new Claim(AdminClaims.MustChangePassword, "1"));
            return identity;
        }
    }

    /// <summary>A deactivated account cannot sign in. Deactivating also renews its security stamp, which ends open sessions.</summary>
    public sealed class AdminSignInManager : SignInManager<AdminUser>
    {
        public AdminSignInManager(UserManager<AdminUser> userManager, IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<AdminUser> claimsFactory, IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<AdminUser>> logger, IAuthenticationSchemeProvider schemes, IUserConfirmation<AdminUser> confirmation)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
        }

        public override async Task<bool> CanSignInAsync(AdminUser user) => user.IsActive && await base.CanSignInAsync(user);
    }
}
