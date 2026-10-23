using System.Security.Claims;
using YelG.DAL.Abstractions;
using YelG.Model.Auth;

namespace YelG.AdminPortal.Infrastructure
{
    public sealed class HttpCurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _accessor;

        public HttpCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

        private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

        public long? UserId => Principal?.UserId();
        public string? UserName => Principal?.Identity?.Name;
        public bool IsAdmin => Principal?.IsInRole(AppRoles.Admin) ?? false;
    }
}
