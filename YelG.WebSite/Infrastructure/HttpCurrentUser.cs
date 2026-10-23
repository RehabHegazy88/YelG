using System.Security.Claims;
using YelG.WebSite.Application.Abstractions;
using YelG.WebSite.Application.Common;

namespace YelG.WebSite.Infrastructure
{
    /// <summary>The signed-in admin user from the request's claims; empty on public pages.</summary>
    public sealed class HttpCurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _accessor;

        public HttpCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

        private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

        public long? UserId => long.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        public string? UserName => Principal?.Identity?.IsAuthenticated == true ? Principal.Identity.Name : null;

        public bool IsAdmin => Principal?.IsInRole(AdminRoles.Admin) ?? false;
    }
}
