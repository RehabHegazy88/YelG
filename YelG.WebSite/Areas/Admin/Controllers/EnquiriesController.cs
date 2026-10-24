using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YelG.WebSite.Application.Abstractions;
using YelG.WebSite.Application.Common;
using YelG.WebSite.Application.Enquiries;
using YelG.WebSite.Areas.Admin.Models;
using YelG.WebSite.Domain.Audit;
using YelG.WebSite.Domain.Enquiries;
using YelG.WebSite.Infrastructure.Data;

namespace YelG.WebSite.Areas.Admin.Controllers
{
    /// <summary>
    /// The consultation requests: Admins and Editors read them, change their status and add internal notes;
    /// only Admins export or delete them (personal data). Exports are written to the audit log.
    /// </summary>
    [Route("admin/enquiries")]
    public sealed class EnquiriesController : AdminController
    {
        private const int PageSize = 30;

        private readonly WebSiteDbContext _db;
        private readonly IEnquiryNotificationQueue _notifications;
        private readonly ICurrentUser _user;
        private readonly TimeProvider _clock;

        public EnquiriesController(WebSiteDbContext db, IEnquiryNotificationQueue notifications, ICurrentUser user, TimeProvider clock)
        {
            _db = db;
            _notifications = notifications;
            _user = user;
            _clock = clock;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] EnquiryFilter filter, int page = 1, CancellationToken ct = default)
        {
            var query = Filter(filter);
            var total = await query.CountAsync(ct);
            var pages = Math.Max(1, (total + PageSize - 1) / PageSize);
            page = Math.Clamp(page, 1, pages);
            return View(new EnquiryListModel
            {
                Filter = filter,
                Items = await query.OrderByDescending(e => e.AddedDate).ThenByDescending(e => e.Id)
                    .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(ct),
                Total = total,
                Page = page,
                PageCount = pages,
                Services = await ServiceOptionsAsync(ct),
                Counts = await _db.Enquiries.AsNoTracking().GroupBy(e => e.Status).Select(g => new { g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.Key, g => g.Count, ct)
            });
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> Details(long id, CancellationToken ct)
        {
            var enquiry = await _db.Enquiries.AsNoTracking().Include(e => e.Notes.OrderBy(n => n.AddedDate))
                .FirstOrDefaultAsync(e => e.Id == id, ct);
            if (enquiry is null) return NotFound();

            var authorIds = enquiry.Notes.Select(n => n.AddedBy_PersonID).OfType<long>().Distinct().ToList();
            var authors = await _db.Users.AsNoTracking().Where(u => authorIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);
            return View(new EnquiryDetailsModel { Enquiry = enquiry, Authors = authors, CurrentUserId = _user.UserId });
        }

        [HttpPost("{id:long}/status")]
        public async Task<IActionResult> Status(long id, EnquiryStatus status, CancellationToken ct)
        {
            if (await _db.Enquiries.FirstOrDefaultAsync(e => e.Id == id, ct) is not { } enquiry) return NotFound();
            if (!Enum.IsDefined(status)) return BadRequest();
            enquiry.Status = status;
            await _db.SaveChangesAsync(ct);
            Flash($"أصبحت حالة الطلب: {EnquiryFormat.Status(status)}.");
            return Redirect($"/admin/enquiries/{id}");
        }

        [HttpPost("{id:long}/notes")]
        public async Task<IActionResult> AddNote(long id, string? text, CancellationToken ct)
        {
            if (!await _db.Enquiries.AnyAsync(e => e.Id == id, ct)) return NotFound();
            text = (text ?? "").Trim().Replace("\r\n", "\n");
            if (text.Length is 0 or > 4000)
            {
                FlashError(text.Length == 0 ? "اكتب نص الملاحظة." : "الملاحظة أطول من 4000 حرف.");
                return Redirect($"/admin/enquiries/{id}#notes");
            }
            _db.EnquiryNotes.Add(new EnquiryNote { EnquiryId = id, Text = text });
            await _db.SaveChangesAsync(ct);
            Flash("تمت إضافة الملاحظة.");
            return Redirect($"/admin/enquiries/{id}#notes");
        }

        /// <summary>A note can be removed by its author or by an Admin.</summary>
        [HttpPost("{id:long}/notes/{noteId:long}/delete")]
        public async Task<IActionResult> DeleteNote(long id, long noteId, CancellationToken ct)
        {
            if (await _db.EnquiryNotes.FirstOrDefaultAsync(n => n.Id == noteId && n.EnquiryId == id, ct) is not { } note) return NotFound();
            if (note.AddedBy_PersonID != _user.UserId && !User.IsInRole(AdminRoles.Admin)) return Forbid();
            _db.EnquiryNotes.Remove(note);
            await _db.SaveChangesAsync(ct);
            Flash("تم حذف الملاحظة.");
            return Redirect($"/admin/enquiries/{id}#notes");
        }

        /// <summary>Sends the notification email again (after the mail settings were fixed, for example).</summary>
        [HttpPost("{id:long}/resend")]
        public async Task<IActionResult> Resend(long id, CancellationToken ct)
        {
            if (await _db.Enquiries.FirstOrDefaultAsync(e => e.Id == id, ct) is not { } enquiry) return NotFound();
            enquiry.NotificationSentAt = null;
            enquiry.NotificationAttempts = 0;
            enquiry.NotificationError = null;
            await _db.SaveChangesAsync(ct);
            _notifications.Enqueue(id);
            Flash("سيُعاد إرسال الإشعار خلال لحظات. حدّث الصفحة لمعرفة النتيجة.");
            return Redirect($"/admin/enquiries/{id}");
        }

        [HttpPost("{id:long}/delete")]
        [Authorize(Policy = AdminPolicies.AdminOnly)]
        public async Task<IActionResult> Delete(long id, CancellationToken ct)
        {
            if (await _db.Enquiries.Include(e => e.Notes).FirstOrDefaultAsync(e => e.Id == id, ct) is not { } enquiry) return NotFound();
            _db.Enquiries.Remove(enquiry);   // a real delete, with its notes: it is personal data
            await _db.SaveChangesAsync(ct);
            Flash("تم حذف الطلب نهائيًا.");
            return Redirect("/admin/enquiries");
        }

        /// <summary>The filtered enquiries as CSV (UTF-8 with BOM, so Excel shows Arabic), recorded in the audit log.</summary>
        [HttpGet("export")]
        [Authorize(Policy = AdminPolicies.AdminOnly)]
        public async Task<IActionResult> Export([FromQuery] EnquiryFilter filter, CancellationToken ct)
        {
            var items = await Filter(filter).OrderByDescending(e => e.AddedDate).ThenByDescending(e => e.Id).ToListAsync(ct);
            var csv = new StringBuilder();
            Row(csv, "Id", "Received (Cairo)", "Status", "Name", "Email", "Company", "Phone", "Service", "Page language",
                "Message", "Self-check score", "Consent (UTC)", "Notification sent (UTC)");
            foreach (var e in items)
                Row(csv, e.Id.ToString(CultureInfo.InvariantCulture), AdminFormat.Time(e.AddedDate), e.Status.ToString(), e.Name, e.Email,
                    e.Company, e.Phone ?? "", e.ServiceLabel, e.Culture, e.Message, e.SelfCheckScore?.ToString(CultureInfo.InvariantCulture) ?? "",
                    e.ConsentAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    e.NotificationSentAt?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "");

            _db.AuditLogs.Add(new AuditLog
            {
                OccurredAt = _clock.GetUtcNow().UtcDateTime,
                UserId = _user.UserId,
                UserName = _user.UserName,
                Action = "Exported",
                EntityType = nameof(Enquiry),
                Changes = System.Text.Json.JsonSerializer.Serialize(new { count = items.Count, filter = filter.ToString() })
            });
            await _db.SaveChangesAsync(ct);

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"enquiries-{_clock.GetUtcNow():yyyy-MM-dd}.csv");
        }

        private IQueryable<Enquiry> Filter(EnquiryFilter f)
        {
            var query = _db.Enquiries.AsNoTracking();
            if (f.Status is { } status) query = query.Where(e => e.Status == status);
            if (f.Service == EnquiryInput.NotSure) query = query.Where(e => e.IsNotSure);
            else if (long.TryParse(f.Service, out var serviceId)) query = query.Where(e => e.ServiceId == serviceId);
            if (!string.IsNullOrWhiteSpace(f.Q))
            {
                var term = f.Q.Trim();
                query = query.Where(e => e.Name.Contains(term) || e.Email.Contains(term) || e.Company.Contains(term));
            }
            if (f.From is { } from) query = query.Where(e => e.AddedDate >= from.ToDateTime(TimeOnly.MinValue));
            if (f.To is { } to) query = query.Where(e => e.AddedDate < to.AddDays(1).ToDateTime(TimeOnly.MinValue));
            return query;
        }

        private async Task<IReadOnlyList<(string Value, string Label)>> ServiceOptionsAsync(CancellationToken ct)
        {
            var used = await _db.Enquiries.AsNoTracking().Where(e => e.ServiceId != null)
                .GroupBy(e => new { e.ServiceId, e.ServiceLabel }).Select(g => g.Key).ToListAsync(ct);
            var options = used.GroupBy(u => u.ServiceId!.Value).Select(g => (g.Key.ToString(CultureInfo.InvariantCulture), g.First().ServiceLabel))
                .OrderBy(o => o.Item2).ToList();
            options.Add((EnquiryInput.NotSure, "لست متأكدًا بعد"));
            return options;
        }

        /// <summary>A CSV row: quoted, and cells that a spreadsheet would run as a formula are made plain text.</summary>
        private static void Row(StringBuilder csv, params string[] cells)
        {
            csv.AppendJoin(',', cells.Select(c =>
            {
                var value = c.Length > 0 && "=+-@\t\r".Contains(c[0]) ? "'" + c : c;
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }));
            csv.Append("\r\n");
        }
    }
}
