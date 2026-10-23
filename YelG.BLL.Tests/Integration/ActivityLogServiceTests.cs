using ClosedXML.Excel;
using YelG.BLL.Common;
using YelG.BLL.Services.Audit;
using YelG.BLL.ViewModel;
using YelG.Model.Audit;

namespace YelG.BLL.Tests.Integration
{
    [Collection(ServiceDatabaseCollection.Name)]
    public class ActivityLogServiceTests
    {
        private readonly ServiceDatabase _db;

        public ActivityLogServiceTests(ServiceDatabase db) => _db = db;

        /// <summary>Writes log rows with a unique marker in their details, so a search finds only this test's rows.</summary>
        private async Task<string> SeedAsync(params (string Action, long? UserId, DateTimeOffset At)[] rows)
        {
            var marker = "سجل-" + Guid.NewGuid().ToString("N")[..8];
            await using var admin = _db.AsAdmin();
            admin.Context.ActivityLogs.AddRange(rows.Select((r, i) => new ActivityLog
            {
                Action = r.Action, UserId = r.UserId, UserName = r.UserId is null ? "مجهول" : "login-name",
                OccurredAt = r.At, Details = $"{marker} #{i}"
            }));
            await admin.Context.SaveChangesAsync();
            return marker;
        }

        [LocalDbFact]
        public async Task Filters_by_cairo_day_user_and_group_and_shows_display_names()
        {
            // 23:30 UTC on Jan 9 is already Jan 10 in Cairo (UTC+2).
            var lateJan9Utc = new DateTimeOffset(2026, 1, 9, 23, 30, 0, TimeSpan.Zero);
            var marker = await SeedAsync(
                ("EVALUATION_APPROVED", _db.AdminId, lateJan9Utc),
                ("LOGIN_FAILED", null, lateJan9Utc.AddHours(-2)),            // Jan 9 in Cairo
                ("SAVE_STATE", _db.ReviewerId, lateJan9Utc.AddDays(1)),
                ("SOMETHING_NEW", _db.ReviewerId, lateJan9Utc.AddDays(1)));

            await using var admin = _db.AsAdmin();
            async Task<ActivityLogPage> Search(DateOnly? from = null, DateOnly? to = null, long? user = null, string? group = null) =>
                await admin.ActivityLog.SearchAsync(new ActivityLogFilter(from, to, user, group, marker));

            var all = await Search();
            Assert.Equal(4, all.Total);
            Assert.Equal(lateJan9Utc.AddDays(1), all.Rows[0].OccurredAt);   // newest first

            var approved = Assert.Single((await Search(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 10))).Rows);
            Assert.Equal("اعتماد تقييم", approved.ActionLabel);
            Assert.Equal(ActivityActions.Evaluations, approved.Group);
            Assert.Equal("مدير النظام", approved.UserName);              // display name, not the login name

            var failed = Assert.Single((await Search(group: ActivityActions.Login)).Rows);
            Assert.Equal("مجهول", failed.UserName);                       // unknown user keeps the name typed at login
            Assert.Null(failed.UserId);

            var reviewer = await Search(user: _db.ReviewerId);
            Assert.Equal(2, reviewer.Total);
            Assert.Contains(reviewer.Rows, r => r is { Action: "SOMETHING_NEW", ActionLabel: "SOMETHING_NEW", Group: "أخرى" });

            // Dates given the wrong way round are swapped rather than matching nothing.
            Assert.Equal(4, (await Search(new DateOnly(2026, 1, 11), new DateOnly(2026, 1, 9))).Total);
        }

        [LocalDbFact]
        public async Task Pages_past_the_end_show_the_last_page_and_reviewers_are_refused()
        {
            var at = DateTimeOffset.UtcNow.AddYears(-1);
            var marker = await SeedAsync(Enumerable.Range(0, ActivityLogService.PageSize + 5)
                .Select(i => ("SAVE_STATE", (long?)_db.ReviewerId, at.AddMinutes(i))).ToArray());

            await using (var reviewer = _db.AsReviewer())
                await Assert.ThrowsAsync<ForbiddenException>(() => reviewer.ActivityLog.SearchAsync(new ActivityLogFilter(null, null, null, null, null)));

            await using var admin = _db.AsAdmin();
            var page = await admin.ActivityLog.SearchAsync(new ActivityLogFilter(null, null, null, null, marker, Page: 9));
            Assert.Equal(2, page.Page);
            Assert.Equal(2, page.PageCount);
            Assert.Equal(5, page.Rows.Count);

            using var book = new XLWorkbook(new MemoryStream(
                (await admin.Exports.ExportActivityLogAsync(new ActivityLogFilter(null, null, null, null, marker))).Content));
            var sheet = book.Worksheet("سجل النشاط");
            Assert.Equal(ActivityLogService.PageSize + 5, sheet.RowsUsed().Count() - 1);
            Assert.Equal("حفظ إجابات التقييم", sheet.Cell(2, 4).GetString());
            Assert.Equal("مقيم أول", sheet.Cell(2, 2).GetString());
        }
    }
}
