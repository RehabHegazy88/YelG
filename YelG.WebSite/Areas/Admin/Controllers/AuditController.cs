using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Areas.Admin.Editing;
using YelG.WebSite.Areas.Admin.Models;

namespace YelG.WebSite.Areas.Admin.Controllers
{
    /// <summary>Who changed what and when (Admins only).</summary>
    [Route("admin/audit")]
    [Authorize(Policy = AdminPolicies.AdminOnly)]
    public sealed class AuditController : AdminController
    {
        private const int PageSize = 50;

        private readonly IAuditLogQuery _audit;
        private readonly EditorRegistry _registry;

        public AuditController(IAuditLogQuery audit, EditorRegistry registry)
        {
            _audit = audit;
            _registry = registry;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? type, string? user, string? entity, DateOnly? from, DateOnly? to, int page = 1,
            CancellationToken ct = default)
        {
            var filter = new AuditLogFilter(type, user, entity, from, to);
            var (types, users) = await _audit.FilterValuesAsync(ct);
            return View(new AuditListModel
            {
                Page = await _audit.PageAsync(filter, page, PageSize, ct),
                Filter = filter,
                EntityTypes = types,
                Users = users,
                Registry = _registry
            });
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> Details(long id, CancellationToken ct)
        {
            if (await _audit.FindAsync(id, ct) is not { } entry) return NotFound();
            ViewBag.Registry = _registry;
            return View(entry);
        }
    }
}
