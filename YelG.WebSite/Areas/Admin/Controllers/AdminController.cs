using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using YelG.WebSite.Infrastructure.Identity;

namespace YelG.WebSite.Areas.Admin.Controllers
{
    /// <summary>Allowed while the account still has its temporary password (sign-out, the password change).</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class AllowTemporaryPasswordAttribute : Attribute
    {
    }

    /// <summary>
    /// Base of the admin panel controllers: signed-in Admins and Editors only, antiforgery on every post,
    /// never cached, and held at the password change until a temporary password is replaced.
    /// </summary>
    [Area("Admin")]
    [Authorize(Policy = AdminPolicies.Content)]
    [AutoValidateAntiforgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public abstract class AdminController : Controller
    {
        public const string FlashKey = "admin.flash";
        public const string FlashErrorKey = "admin.flash.error";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var allowed = context.ActionDescriptor.EndpointMetadata.OfType<AllowTemporaryPasswordAttribute>().Any()
                          || context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
            if (!allowed && User.HasClaim(c => c.Type == AdminClaims.MustChangePassword))
                context.Result = RedirectToAction("ChangePassword", "Account", new { area = "Admin" });
            base.OnActionExecuting(context);
        }

        protected void Flash(string message) => TempData[FlashKey] = message;

        protected void FlashError(string message) => TempData[FlashErrorKey] = message;

        /// <summary>A local return address, or the fallback.</summary>
        protected string SafeReturn(string? url, string fallback) => !string.IsNullOrEmpty(url) && Url.IsLocalUrl(url) ? url : fallback;
    }

    public static class AdminPolicies
    {
        /// <summary>Content, media, preview: Admins and Editors.</summary>
        public const string Content = "admin.content";

        /// <summary>Users, site settings, the audit log.</summary>
        public const string AdminOnly = "admin.only";
    }
}
