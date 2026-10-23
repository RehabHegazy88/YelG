using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace YelG.AdminPortal.Infrastructure
{
    /// <summary>
    /// Holds an account that still has a temporary password (set by an admin or by the legacy import)
    /// at the change-password page until its holder chooses their own. Signing out stays possible.
    /// </summary>
    public sealed class MustChangePasswordFilter : IAuthorizationFilter
    {
        private static readonly HashSet<string> AllowedAccountActions =
            new(StringComparer.OrdinalIgnoreCase) { "ChangePassword", "Logout", "Login", "AccessDenied" };

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true || !user.MustChangePassword()) return;

            var values = context.RouteData.Values;
            if (string.Equals(values["controller"] as string, "Account", StringComparison.OrdinalIgnoreCase)
                && AllowedAccountActions.Contains(values["action"] as string ?? string.Empty))
                return;

            context.Result = context.HttpContext.Request.IsAjax()
                ? new JsonResult(new { error = "غيّري كلمة المرور المؤقتة أولًا." }) { StatusCode = StatusCodes.Status403Forbidden }
                : new RedirectToActionResult("ChangePassword", "Account", null);
        }
    }
}
