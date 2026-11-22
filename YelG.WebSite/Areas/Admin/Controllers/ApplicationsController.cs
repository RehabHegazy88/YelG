using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YelG.WebSite.Application.Abstractions;
using YelG.WebSite.Application.Careers;
using YelG.WebSite.Application.Common;
using YelG.WebSite.Areas.Admin.Models;
using YelG.WebSite.Domain.Audit;
using YelG.WebSite.Domain.Careers;
using YelG.WebSite.Domain.Sections;
using YelG.WebSite.Infrastructure.Data;

namespace YelG.WebSite.Areas.Admin.Controllers
{
    /// <summary>
    /// Job applications from the careers pages: Admins and Editors read them, open the CV, change the status and
    /// add internal notes; only Admins export or delete them (personal data). CV downloads and exports are written
    /// to the audit log.
    /// </summary>
    [Route("admin/applications")]
    public sealed class ApplicationsController : AdminController
    {
        private const int PageSize = 30;

        /// <summary>The filter value for general applications (no role).</summary>
        private const string GeneralRole = "general";

        private readonly WebSiteDbContext _db;
        private readonly ICvStore _cvs;
        private readonly IApplicationNotificationQueue _notifications;
        private readonly ICurrentUser _user;
        private readonly TimeProvider _clock;

        public ApplicationsController(WebSiteDbContext db, ICvStore cvs, IApplicationNotificationQueue notifications, ICurrentUser user,
            TimeProvider clock)
        {
            _db = db;
            _cvs = cvs;
            _notifications = notifications;
            _user = user;
            _clock = clock;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] ApplicationFilter filter, int page = 1, CancellationToken ct = default)
        {
            var questions = await QuestionsAsync(filter, ct);
            var query = Filter(filter, questions);
            var total = await query.CountAsync(ct);
            var pages = Math.Max(1, (total + PageSize - 1) / PageSize);
            page = Math.Clamp(page, 1, pages);
            var ordered = query.OrderByDescending(a => a.AddedDate).ThenByDescending(a => a.Id).Skip((page - 1) * PageSize).Take(PageSize);
            return View(new ApplicationListModel
            {
                Filter = filter,
                Questions = questions,
                Items = questions.Count > 0 ? await ordered.Include(a => a.Answers).ToListAsync(ct) : await ordered.ToListAsync(ct),
                Total = total,
                Page = page,
                PageCount = pages,
                Roles = await RoleOptionsAsync(ct),
                Counts = await _db.JobApplications.AsNoTracking().GroupBy(a => a.Status).Select(g => new { g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.Key, g => g.Count, ct)
            });
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> Details(long id, CancellationToken ct)
        {
            var application = await _db.JobApplications.AsNoTracking().Include(a => a.Notes.OrderBy(n => n.AddedDate))
                .Include(a => a.Answers.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)).AsSplitQuery()
                .FirstOrDefaultAsync(a => a.Id == id, ct);
            if (application is null) return NotFound();

            var authorIds = application.Notes.Select(n => n.AddedBy_PersonID).OfType<long>().Distinct().ToList();
            var authors = await _db.Users.AsNoTracking().Where(u => authorIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);
            var roleSlug = application.CareerRoleId is { } roleId
                ? await _db.CareerRoles.AsNoTracking().Where(r => r.Id == roleId).Select(r => r.Slug).FirstOrDefaultAsync(ct)
                : null;
            return View(new ApplicationDetailsModel { Application = application, Authors = authors, CurrentUserId = _user.UserId, RoleSlug = roleSlug });
        }

        /// <summary>The CV, as a download (never shown inline), recorded in the audit log.</summary>
        [HttpGet("{id:long}/cv")]
        public async Task<IActionResult> Cv(long id, CancellationToken ct)
        {
            var application = await _db.JobApplications.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
            if (application is null) return NotFound();
            var stream = _cvs.OpenRead(application.CvStoredPath);
            if (stream is null) return NotFound();

            Audit("Downloaded", id.ToString(CultureInfo.InvariantCulture), new { file = "cv" });
            await _db.SaveChangesAsync(ct);
            Response.Headers.XContentTypeOptions = "nosniff";
            return File(stream, application.CvContentType, application.CvFileName);
        }

        [HttpPost("{id:long}/status")]
        public async Task<IActionResult> Status(long id, ApplicationStatus status, CancellationToken ct)
        {
            if (await _db.JobApplications.FirstOrDefaultAsync(a => a.Id == id, ct) is not { } application) return NotFound();
            if (!Enum.IsDefined(status)) return BadRequest();
            application.Status = status;
            await _db.SaveChangesAsync(ct);
            Flash($"أصبحت حالة الطلب: {ApplicationFormat.Status(status)}.");
            return Redirect($"/admin/applications/{id}");
        }

        [HttpPost("{id:long}/notes")]
        public async Task<IActionResult> AddNote(long id, string? text, CancellationToken ct)
        {
            if (!await _db.JobApplications.AnyAsync(a => a.Id == id, ct)) return NotFound();
            text = (text ?? "").Trim().Replace("\r\n", "\n");
            if (text.Length is 0 or > 4000)
            {
                FlashError(text.Length == 0 ? "اكتب نص الملاحظة." : "الملاحظة أطول من 4000 حرف.");
                return Redirect($"/admin/applications/{id}#notes");
            }
            _db.JobApplicationNotes.Add(new JobApplicationNote { JobApplicationId = id, Text = text });
            await _db.SaveChangesAsync(ct);
            Flash("تمت إضافة الملاحظة.");
            return Redirect($"/admin/applications/{id}#notes");
        }

        [HttpPost("{id:long}/notes/{noteId:long}/delete")]
        public async Task<IActionResult> DeleteNote(long id, long noteId, CancellationToken ct)
        {
            if (await _db.JobApplicationNotes.FirstOrDefaultAsync(n => n.Id == noteId && n.JobApplicationId == id, ct) is not { } note) return NotFound();
            if (note.AddedBy_PersonID != _user.UserId && !User.IsInRole(AdminRoles.Admin)) return Forbid();
            _db.JobApplicationNotes.Remove(note);
            await _db.SaveChangesAsync(ct);
            Flash("تم حذف الملاحظة.");
            return Redirect($"/admin/applications/{id}#notes");
        }

        [HttpPost("{id:long}/resend")]
        public async Task<IActionResult> Resend(long id, CancellationToken ct)
        {
            if (await _db.JobApplications.FirstOrDefaultAsync(a => a.Id == id, ct) is not { } application) return NotFound();
            application.NotificationSentAt = null;
            application.NotificationAttempts = 0;
            application.NotificationError = null;
            await _db.SaveChangesAsync(ct);
            _notifications.Enqueue(id);
            Flash("سيُعاد إرسال الإشعار خلال لحظات. حدّث الصفحة لمعرفة النتيجة.");
            return Redirect($"/admin/applications/{id}");
        }

        /// <summary>A real delete, with its notes and its CV file: it is personal data.</summary>
        [HttpPost("{id:long}/delete")]
        [Authorize(Policy = AdminPolicies.AdminOnly)]
        public async Task<IActionResult> Delete(long id, CancellationToken ct)
        {
            if (await _db.JobApplications.Include(a => a.Notes).FirstOrDefaultAsync(a => a.Id == id, ct) is not { } application) return NotFound();
            var cv = application.CvStoredPath;
            _db.JobApplications.Remove(application);
            await _db.SaveChangesAsync(ct);
            _cvs.Delete(cv);
            Flash("تم حذف الطلب وسيرته الذاتية نهائيًا.");
            return Redirect("/admin/applications");
        }

        /// <summary>The filtered applications as CSV (UTF-8 with BOM), without the CVs; recorded in the audit log.</summary>
        [HttpGet("export")]
        [Authorize(Policy = AdminPolicies.AdminOnly)]
        public async Task<IActionResult> Export([FromQuery] ApplicationFilter filter, CancellationToken ct)
        {
            var questions = await QuestionsAsync(filter, ct);
            var items = await Filter(filter, questions).OrderByDescending(a => a.AddedDate).ThenByDescending(a => a.Id)
                .Include(a => a.Answers.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)).ToListAsync(ct);
            var csv = new StringBuilder();
            // With a role selected, one column per question; otherwise all answers in one column.
            Row(csv, new[] { "Id", "Received (Cairo)", "Status", "Role", "Name", "Email", "Phone", "LinkedIn", "Page language", "CV file", "Message",
                "Consent (UTC)" }.Concat(questions.Count > 0 ? questions.Select(q => q.Text.Ar) : ["Answers"]).ToArray());
            foreach (var a in items)
            {
                var answers = questions.Count > 0
                    ? questions.Select(q => ApplicationFormat.Answers(a.Answers.Where(x => x.CareerRoleQuestionId == q.Id)))
                    : [string.Join(" | ", a.Answers.GroupBy(x => (x.SortOrder, x.Question)).Select(g => g.Key.Question + ": " + ApplicationFormat.Answers(g)))];
                Row(csv, new[] { a.Id.ToString(CultureInfo.InvariantCulture), AdminFormat.Time(a.AddedDate), a.Status.ToString(), a.RoleTitle, a.Name, a.Email,
                    a.Phone ?? "", a.LinkedInUrl ?? "", a.Culture, a.CvFileName, a.Message ?? "",
                    a.ConsentAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) }.Concat(answers).ToArray());
            }

            Audit("Exported", null, new { count = items.Count, filter = filter.ToString() });
            await _db.SaveChangesAsync(ct);
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"applications-{_clock.GetUtcNow():yyyy-MM-dd}.csv");
        }

        /// <summary>The roles that received applications, for the filter (by id, named as the applicants saw them).</summary>
        private async Task<IReadOnlyList<(string Value, string Label)>> RoleOptionsAsync(CancellationToken ct)
        {
            var used = await _db.JobApplications.AsNoTracking().Where(a => a.CareerRoleId != null)
                .GroupBy(a => new { a.CareerRoleId, a.RoleTitle }).Select(g => g.Key).ToListAsync(ct);
            var options = used.GroupBy(u => u.CareerRoleId!.Value)
                .Select(g => (g.Key.ToString(CultureInfo.InvariantCulture), g.First().RoleTitle)).OrderBy(r => r.Item2).ToList();
            options.Add((GeneralRole, "طلب عام (بدون وظيفة)"));
            return options;
        }

        /// <summary>The selected role's questions (also hidden ones, which older applications may have answered).</summary>
        private async Task<List<CareerRoleQuestion>> QuestionsAsync(ApplicationFilter f, CancellationToken ct) =>
            long.TryParse(f.Role, out var roleId)
                ? await _db.CareerRoleQuestions.AsNoTracking().Where(q => q.CareerRoleId == roleId).OrderBy(q => q.SortOrder).ThenBy(q => q.Id).ToListAsync(ct)
                : [];

        private IQueryable<JobApplication> Filter(ApplicationFilter f, IReadOnlyList<CareerRoleQuestion> questions)
        {
            var query = _db.JobApplications.AsNoTracking();
            foreach (var q in questions)
            {
                if (f.Answer(q.Id) is not { } value) continue;
                var questionId = q.Id;
                query = q.Kind is QuestionKind.ShortText or QuestionKind.LongText
                    ? query.Where(a => a.Answers.Any(x => x.CareerRoleQuestionId == questionId && x.Value.Contains(value)))
                    : query.Where(a => a.Answers.Any(x => x.CareerRoleQuestionId == questionId && x.Value == value));
            }
            if (f.Status is { } status) query = query.Where(a => a.Status == status);
            if (f.Role == GeneralRole) query = query.Where(a => a.CareerRoleId == null);
            else if (long.TryParse(f.Role, out var roleId)) query = query.Where(a => a.CareerRoleId == roleId);
            if (!string.IsNullOrWhiteSpace(f.Q))
            {
                var term = f.Q.Trim();
                query = query.Where(a => a.Name.Contains(term) || a.Email.Contains(term));
            }
            if (f.From is { } from) query = query.Where(a => a.AddedDate >= from.ToDateTime(TimeOnly.MinValue));
            if (f.To is { } to) query = query.Where(a => a.AddedDate < to.AddDays(1).ToDateTime(TimeOnly.MinValue));
            return query;
        }

        private void Audit(string action, string? entityId, object changes) => _db.AuditLogs.Add(new AuditLog
        {
            OccurredAt = _clock.GetUtcNow().UtcDateTime,
            UserId = _user.UserId,
            UserName = _user.UserName,
            Action = action,
            EntityType = nameof(JobApplication),
            EntityId = entityId,
            Changes = System.Text.Json.JsonSerializer.Serialize(changes)
        });

        /// <summary>A CSV row: quoted, and cells a spreadsheet would run as a formula are made plain text.</summary>
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
