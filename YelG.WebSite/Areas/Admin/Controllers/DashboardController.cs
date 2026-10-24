using Microsoft.AspNetCore.Mvc;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Application.Common;
using YelG.WebSite.Areas.Admin.Editing;
using YelG.WebSite.Areas.Admin.Models;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Media;

namespace YelG.WebSite.Areas.Admin.Controllers
{
    [Route("admin")]
    public sealed class DashboardController : AdminController
    {
        private readonly EditorRegistry _registry;
        private readonly IContentStore _store;
        private readonly IAuditLogQuery _audit;

        public DashboardController(EditorRegistry registry, IContentStore store, IAuditLogQuery audit)
        {
            _registry = registry;
            _store = store;
            _audit = audit;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var tiles = new List<DashboardTile>();
            foreach (var screen in _registry.Screens.Where(s => !s.AdminOnly || User.IsInRole(AdminRoles.Admin)))
            {
                var main = screen.LoadMain is null ? null : await screen.LoadMain(_store, ct);
                bool? published = main?.GetType().GetProperty("IsPublished")?.GetValue(main) as bool?;
                var items = 0;
                DateTime? modified = main?.LastModifiedDate;
                foreach (var list in screen.Lists)
                {
                    var rows = await list.ListAsync(_store, null, ct);
                    items += rows.Count;
                    if (rows.Count > 0 && (modified is null || rows.Max(r => r.LastModifiedDate) > modified))
                        modified = rows.Max(r => r.LastModifiedDate);
                }
                tiles.Add(new DashboardTile(screen, published, items, modified));
            }

            var recent = User.IsInRole(AdminRoles.Admin)
                ? (await _audit.PageAsync(new AuditLogFilter(), 1, 8, ct)).Items
                : [];
            return View(new DashboardModel
            {
                Tiles = tiles,
                Recent = recent,
                MediaCount = await _store.CountAsync<MediaAsset>(ct: ct),
                Registry = _registry
            });
        }
    }
}
