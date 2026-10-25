using Microsoft.AspNetCore.Mvc;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Application.Common;
using YelG.WebSite.Areas.Admin.Editing;
using YelG.WebSite.Areas.Admin.Models;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Enquiries;
using YelG.WebSite.Domain.Media;
using YelG.WebSite.Domain.Sections;
using YelG.WebSite.Infrastructure.Email;

namespace YelG.WebSite.Areas.Admin.Controllers
{
    [Route("admin")]
    public sealed class DashboardController : AdminController
    {
        private readonly EditorRegistry _registry;
        private readonly IContentStore _store;
        private readonly IAuditLogQuery _audit;
        private readonly SmtpOptions _smtp;

        public DashboardController(EditorRegistry registry, IContentStore store, IAuditLogQuery audit,
            Microsoft.Extensions.Options.IOptions<SmtpOptions> smtp)
        {
            _smtp = smtp.Value;
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
                NewEnquiries = await _store.CountAsync<Enquiry>(e => e.Status == EnquiryStatus.New, ct),
                NewApplications = await _store.CountAsync<Domain.Careers.JobApplication>(a => a.Status == Domain.Careers.ApplicationStatus.New, ct),
                Warnings = await WarningsAsync(ct),
                Registry = _registry
            });
        }

        private async Task<List<(string, string?)>> WarningsAsync(CancellationToken ct)
        {
            var warnings = new List<(string, string?)>();
            if (!_smtp.IsConfigured)
                warnings.Add(("خادم البريد غير مُعدّ (Smtp)، فلن تصل إشعارات الطلبات بالبريد. الطلبات تُحفظ هنا على أي حال.", null));
            var failed = await _store.CountAsync<Enquiry>(e => e.NotificationSentAt == null && e.NotificationAttempts > 0, ct)
                         + await _store.CountAsync<Domain.Careers.JobApplication>(a => a.NotificationSentAt == null && a.NotificationAttempts > 0, ct);
            if (failed > 0) warnings.Add(($"تعذّر إرسال إشعار البريد لـ {failed} من طلبات الاستشارة والتوظيف.", "/admin/enquiries"));
            var consult = await _store.FirstAsync<ConsultSection>(ct);
            if (consult is not null && (consult.SuccessTitle.IsEmpty || consult.FailureText.IsEmpty))
                warnings.Add(("رسائل نجاح وفشل إرسال النموذج لم تُكتب بعد؛ تظهر صياغة مؤقتة حتى تعتمدها الشركة.", "/admin/sections/consult"));
            return warnings;
        }
    }
}
