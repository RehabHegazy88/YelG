using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using YelG.BLL.Common;
using YelG.BLL.ViewModel;
using YelG.Model.Enums;

namespace YelG.BLL.Tests.Integration
{
    [Collection(ServiceDatabaseCollection.Name)]
    public class ExcelExportServiceTests
    {
        private readonly ServiceDatabase _db;

        public ExcelExportServiceTests(ServiceDatabase db) => _db = db;

        /// <summary>
        /// A new company evaluated by reviewer 1 with the given answers saved (the admin gets an
        /// empty evaluation of it too). Submitted when asked, so the admin may update its scope.
        /// </summary>
        private async Task<(long EvaluationId, string Company)> EvaluationWithAsync(
            Dictionary<string, IReadOnlyList<string>> answers, bool submit = false)
        {
            var name = "تصدير " + Guid.NewGuid().ToString("N")[..8];
            long evaluationId;
            await using (var admin = _db.AsAdmin())
            {
                var company = await admin.Companies.CreateAsync(new CreateCompanyRequest(name, 1, new[] { _db.ReviewerId }));
                evaluationId = await admin.Context.Evaluations
                    .Where(e => e.CompanyId == company.CompanyId && e.ReviewerId == _db.ReviewerId).Select(e => e.Id).SingleAsync();
            }
            await using var reviewer = _db.AsReviewer();
            var opened = await reviewer.Evaluations.GetAsync(evaluationId);
            var saved = await reviewer.Evaluations.SaveAsync(evaluationId, new SaveEvaluationRequest(answers, null, null, opened.RowVersion));
            if (submit) await reviewer.Evaluations.SubmitAsync(evaluationId, saved.RowVersion);
            return (evaluationId, name);
        }

        private static XLWorkbook Open(ExportFile file)
        {
            Assert.EndsWith(".xlsx", file.FileName);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
            return new XLWorkbook(new MemoryStream(file.Content));
        }

        /// <summary>Data rows (header skipped) whose first column is <paramref name="key"/>, optionally of one reviewer.</summary>
        private static List<IXLRow> RowsOf(IXLWorksheet sheet, string key, string? reviewer = null) =>
            sheet.RowsUsed().Skip(1)
                .Where(r => r.Cell(1).GetString() == key)
                .Where(r => reviewer is null || r.Cell(ColumnOf(sheet, "المقيم")).GetString() == reviewer)
                .ToList();

        private static int ColumnOf(IXLWorksheet sheet, string header) =>
            sheet.Row(1).CellsUsed().Single(c => c.GetString() == header).Address.ColumnNumber;

        [LocalDbFact]
        public async Task Results_show_the_score_and_answers_and_only_the_reviewers_own_evaluations()
        {
            var (_, company) = await EvaluationWithAsync(new() { ["C09"] = new[] { "نعم" } });

            await using (var other = _db.AsReviewer2())
            {
                using var otherBook = Open(await other.Exports.ExportResultsAsync());
                Assert.Empty(RowsOf(otherBook.Worksheet("ملخص التقييمات"), company));
            }

            await using var admin = _db.AsAdmin();
            using var book = Open(await admin.Exports.ExportResultsAsync());
            Assert.Equal(new[] { "ملخص التقييمات", "نتائج الفئات", "الإجابات" }, book.Worksheets.Select(w => w.Name));

            var summary = book.Worksheet("ملخص التقييمات");
            Assert.True(summary.RightToLeft);
            Assert.Equal(2, RowsOf(summary, company).Count);   // the reviewer's and the admin's own
            var row = Assert.Single(RowsOf(summary, company, "مقيم أول"));
            Assert.Equal("مسودة", row.Cell(ColumnOf(summary, "الحالة")).GetString());
            Assert.Equal(1, row.Cell(ColumnOf(summary, "الأسئلة المجابة")).GetDouble());
            Assert.Equal(XLDataType.Number, row.Cell(ColumnOf(summary, "درجة الثقة %")).DataType);

            Assert.NotEmpty(RowsOf(book.Worksheet("نتائج الفئات"), company, "مقيم أول"));

            var answers = book.Worksheet("الإجابات");
            var answer = Assert.Single(RowsOf(answers, company));   // the admin's evaluation has no answers
            Assert.Equal("C09", answer.Cell(ColumnOf(answers, "الكود")).GetString());
            Assert.Equal("نعم", answer.Cell(ColumnOf(answers, "الإجابة")).GetString());

            Assert.True(await admin.Context.ActivityLogs.AnyAsync(l => l.Action == "EXPORT_EXCEL" && l.UserId == _db.AdminId));
        }

        [LocalDbFact]
        public async Task Scope_of_one_evaluation_lists_its_tasks_with_hours_and_state()
        {
            var (evaluationId, company) = await EvaluationWithAsync(new() { ["C09"] = new[] { "نعم" } }, submit: true);

            await using (var other = _db.AsReviewer2())
                await Assert.ThrowsAsync<ForbiddenException>(() => other.Exports.ExportScopeAsync(evaluationId));

            await using var admin = _db.AsAdmin();
            var testTemplateId = await admin.Context.TaskTemplates.Where(t => t.Code == "T-TEST").Select(t => t.Id).SingleAsync();
            await admin.Scope.UpdateItemAsync(evaluationId, new UpdateScopeItemRequest(
                testTemplateId, false, ScopeItemStatus.InProgress, TaskOwner.Client, new DateOnly(2026, 12, 31), "محضر اجتماع", false));

            var file = await admin.Exports.ExportScopeAsync(evaluationId);
            Assert.StartsWith($"YELG-Scope-of-Work-{evaluationId}-", file.FileName);
            using var book = Open(file);
            Assert.Equal(new[] { "ملخص النطاق", "نطاق العمل", "توصيات وخدمات اختيارية", "أساس التسعير" }, book.Worksheets.Select(w => w.Name));

            var tasks = book.Worksheet("نطاق العمل");
            var rows = RowsOf(tasks, company);
            Assert.NotEmpty(rows);
            Assert.All(rows, r => Assert.Equal("مقيم أول", r.Cell(2).GetString()));   // only the evaluation asked for
            Assert.All(rows, r => Assert.Equal("بانتظار الاعتماد", r.Cell(3).GetString()));

            var test = Assert.Single(rows, r => r.Cell(ColumnOf(tasks, "كود المهمة")).GetString() == "T-TEST");
            Assert.Equal(2, test.Cell(ColumnOf(tasks, "Team Leader")).GetDouble());
            Assert.Equal("العميل", test.Cell(ColumnOf(tasks, "جهة التنفيذ")).GetString());
            Assert.Equal("قيد التنفيذ", test.Cell(ColumnOf(tasks, "حالة التنفيذ")).GetString());
            Assert.Equal(new DateTime(2026, 12, 31), test.Cell(ColumnOf(tasks, "الموعد المستهدف")).GetDateTime());

            var summary = Assert.Single(RowsOf(book.Worksheet("ملخص النطاق"), company));
            Assert.Equal(rows.Count, summary.Cell(5).GetDouble());
        }

        [LocalDbFact]
        public async Task Question_bank_and_task_library_are_for_admins_and_list_everything()
        {
            await using (var reviewer = _db.AsReviewer())
            {
                await Assert.ThrowsAsync<ForbiddenException>(() => reviewer.Exports.ExportTaskLibraryAsync());
                await Assert.ThrowsAsync<ForbiddenException>(() => reviewer.Exports.ExportQuestionBankAsync(1));
            }

            await using var admin = _db.AsAdmin();
            var version = await admin.Context.QuestionBankVersions
                .Where(v => v.Status == QuestionBankVersionStatus.Published)
                .Select(v => new { v.Id, v.VersionNo, Questions = v.Categories.SelectMany(c => c.Questions).Count(), Categories = v.Categories.Count() })
                .SingleAsync();

            var bankFile = await admin.Exports.ExportQuestionBankAsync(version.Id);
            Assert.StartsWith($"YELG-Question-Bank-v{version.VersionNo}-", bankFile.FileName);
            using (var bank = Open(bankFile))
            {
                Assert.Equal(version.Categories, bank.Worksheet("الفئات والأوزان").RowsUsed().Count() - 1);
                Assert.Equal(version.Questions, bank.Worksheet("الأسئلة").RowsUsed().Count() - 1);
                Assert.True(bank.Worksheet("الاختيارات").RowsUsed().Count() > version.Questions);
            }

            var templates = await admin.Context.TaskTemplates.CountAsync();
            var rules = await admin.Context.AnswerRules.CountAsync();
            using var library = Open(await admin.Exports.ExportTaskLibraryAsync());
            var sheet = library.Worksheet("المهام");
            Assert.Equal(templates, sheet.RowsUsed().Count() - 1);
            Assert.Equal(2, Assert.Single(RowsOf(sheet, "T-TEST")).Cell(ColumnOf(sheet, "Team Leader")).GetDouble());
            Assert.Equal(rules, library.Worksheet("تأثير الإجابات").RowsUsed().Count() - 1);
            Assert.Equal(6, library.Worksheet("المعدلات").RowsUsed().Count() - 1);
        }
    }
}
