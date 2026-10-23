using Microsoft.EntityFrameworkCore;
using YelG.BLL.Common;
using YelG.BLL.ViewModel;
using YelG.Model.Enums;

namespace YelG.BLL.Tests.Integration
{
    /// <summary>
    /// Each test leaves no draft behind (publish or discard), because the database allows one draft at a time
    /// and the other service tests share it.
    /// </summary>
    [Collection(ServiceDatabaseCollection.Name)]
    public class QuestionBankAdminServiceTests
    {
        private readonly ServiceDatabase _db;

        public QuestionBankAdminServiceTests(ServiceDatabase db) => _db = db;

        private static SaveQuestionRequest ToRequest(QuestionEditor q, string categoryCode) => new(
            categoryCode, q.Code, q.Text, q.AnswerType, q.Kind, q.Direction, q.IsCritical, q.AllowCustomAnswer, q.IsActive,
            q.Weights, q.ParentCode, q.ConditionOperator, q.ConditionValue, q.Options, q.Bands);

        private static (QuestionEditor Question, CategoryEditor Category) Find(QuestionBankEditor editor, string code)
        {
            var category = editor.Categories.Single(c => c.Questions.Any(q => q.Code == code));
            return (category.Questions.Single(q => q.Code == code), category);
        }

        [LocalDbFact]
        public async Task Draft_is_a_full_copy_and_creating_it_twice_returns_the_same_draft()
        {
            await using var admin = _db.AsAdmin();
            var publishedId = (await admin.QuestionBank.GetOverviewAsync()).PublishedId!.Value;
            var draftId = await admin.QuestionBank.CreateDraftAsync();
            try
            {
                Assert.Equal(draftId, await admin.QuestionBank.CreateDraftAsync());

                var published = await admin.QuestionBank.GetEditorAsync(publishedId);
                var draft = await admin.QuestionBank.GetEditorAsync(draftId);
                Assert.False(published.IsEditable);
                Assert.True(draft.IsEditable);
                Assert.Equal(
                    published.Categories.SelectMany(c => c.Questions).Select(q => (q.Code, q.ParentCode, q.Options.Count)),
                    draft.Categories.SelectMany(c => c.Questions).Select(q => (q.Code, q.ParentCode, q.Options.Count)));
                Assert.Equal("A03", Find(draft, "A04").Question.ParentCode);
            }
            finally
            {
                await admin.QuestionBank.DiscardDraftAsync(draftId);
            }

            Assert.Null((await admin.QuestionBank.GetOverviewAsync()).DraftId);
        }

        [LocalDbFact]
        public async Task Published_version_cannot_be_edited()
        {
            await using var admin = _db.AsAdmin();
            var overview = await admin.QuestionBank.GetOverviewAsync();
            var editor = await admin.QuestionBank.GetEditorAsync(overview.PublishedId!.Value);
            var (question, category) = Find(editor, "C01");

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                admin.QuestionBank.SaveQuestionAsync(editor.VersionId, question.Id, ToRequest(question, category.Code) with { Text = "تعديل" }));
        }

        [LocalDbFact]
        public async Task Publishing_a_draft_leaves_existing_evaluations_on_their_version()
        {
            long oldEvaluationId, oldVersionId;
            await using (var admin = _db.AsAdmin())
            {
                var company = await admin.Companies.CreateAsync(new CreateCompanyRequest("قبل النشر " + Guid.NewGuid().ToString("N")[..6], 1, Array.Empty<long>()));
                oldEvaluationId = await admin.Context.Evaluations.Where(e => e.CompanyId == company.CompanyId).Select(e => e.Id).SingleAsync();
                oldVersionId = (await admin.QuestionBank.GetOverviewAsync()).PublishedId!.Value;

                var draftId = await admin.QuestionBank.CreateDraftAsync();
                var draft = await admin.QuestionBank.GetEditorAsync(draftId);
                var (c01, category) = Find(draft, "C01");
                await admin.QuestionBank.SaveQuestionAsync(draftId, c01.Id, ToRequest(c01, category.Code) with { Text = "صياغة جديدة للسؤال C01" });
                await admin.QuestionBank.SaveQuestionAsync(draftId, null, new SaveQuestionRequest(
                    category.Code, "Z99", "سؤال جديد للاختبار", AnswerType.YesNo, QuestionKind.Binary, QuestionDirection.Positive,
                    false, false, true, new[] { 3m, 2m, 1m }, "C01", ConditionOperator.Equals, "نعم",
                    new[] { new OptionInput("نعم", 1), new OptionInput("لا", 0) }, Array.Empty<BandInput>()));

                var result = await admin.QuestionBank.PublishAsync(draftId, "اختبار النشر");
                Assert.Equal(draftId, result.VersionId);
            }

            await using var after = _db.AsAdmin();
            var overview = await after.QuestionBank.GetOverviewAsync();
            Assert.NotEqual(oldVersionId, overview.PublishedId);
            Assert.Equal(QuestionBankVersionStatus.Archived, overview.Versions.Single(v => v.Id == oldVersionId).Status);

            var oldEvaluation = await after.Evaluations.GetAsync(oldEvaluationId);
            Assert.Equal(oldVersionId, oldEvaluation.QuestionBankVersionId);

            var company2 = await after.Companies.CreateAsync(new CreateCompanyRequest("بعد النشر " + Guid.NewGuid().ToString("N")[..6], 1, Array.Empty<long>()));
            var newEvaluationId = await after.Context.Evaluations.Where(e => e.CompanyId == company2.CompanyId).Select(e => e.Id).SingleAsync();
            var newEvaluation = await after.Evaluations.GetAsync(newEvaluationId);
            Assert.Equal(overview.PublishedId, newEvaluation.QuestionBankVersionId);

            var preview = await after.Evaluations.PreviewAsync(newEvaluationId,
                new Dictionary<string, IReadOnlyList<string>> { ["C01"] = new[] { "نعم" } });
            Assert.Contains("Z99", preview.VisibleCodes);
        }

        [LocalDbFact]
        public async Task Invalid_question_edits_are_rejected()
        {
            await using var admin = _db.AsAdmin();
            var draftId = await admin.QuestionBank.CreateDraftAsync();
            try
            {
                var draft = await admin.QuestionBank.GetEditorAsync(draftId);
                var (a03, categoryA) = Find(draft, "A03");
                var (a04, _) = Find(draft, "A04");

                // A03 → A04 would loop, because A04 already branches from A03.
                await Assert.ThrowsAsync<BusinessRuleException>(() => admin.QuestionBank.SaveQuestionAsync(draftId, a03.Id,
                    ToRequest(a03, categoryA.Code) with { ParentCode = "A04", ConditionValue = "1" }));

                // A04 and A05 branch on "نعم"; removing that option would hide them forever.
                await Assert.ThrowsAsync<BusinessRuleException>(() => admin.QuestionBank.SaveQuestionAsync(draftId, a03.Id,
                    ToRequest(a03, categoryA.Code) with { Options = new[] { new OptionInput("موجود", 1), new OptionInput("لا", 0) } }));

                // Condition value must be one of the parent's options.
                await Assert.ThrowsAsync<BusinessRuleException>(() => admin.QuestionBank.SaveQuestionAsync(draftId, a04.Id,
                    ToRequest(a04, categoryA.Code) with { ConditionValue = "ربما" }));

                // Overlapping score bands on a numeric question.
                await Assert.ThrowsAsync<BusinessRuleException>(() => admin.QuestionBank.SaveQuestionAsync(draftId, a04.Id,
                    ToRequest(a04, categoryA.Code) with { Bands = new[] { new BandInput(0, 10, 1), new BandInput(5, null, 0) } }));

                // Duplicate code and a choice question with a single option.
                await Assert.ThrowsAsync<BusinessRuleException>(() => admin.QuestionBank.SaveQuestionAsync(draftId, null,
                    ToRequest(a03, categoryA.Code)));
                await Assert.ThrowsAsync<BusinessRuleException>(() => admin.QuestionBank.SaveQuestionAsync(draftId, null,
                    ToRequest(a03, categoryA.Code) with { Code = "A99", ParentCode = null, Options = new[] { new OptionInput("نعم", 1) } }));

                // Valid bands are stored in order.
                await admin.QuestionBank.SaveQuestionAsync(draftId, a04.Id,
                    ToRequest(a04, categoryA.Code) with { Bands = new[] { new BandInput(5, null, 0), new BandInput(null, 5, 1) } });
                var saved = Find(await admin.QuestionBank.GetEditorAsync(draftId), "A04").Question;
                Assert.Equal(new decimal?[] { null, 5 }, saved.Bands.Select(b => b.MinValue));
            }
            finally
            {
                await admin.QuestionBank.DiscardDraftAsync(draftId);
            }
        }

        [LocalDbFact]
        public async Task Deleting_a_question_makes_its_children_unconditional()
        {
            await using var admin = _db.AsAdmin();
            var draftId = await admin.QuestionBank.CreateDraftAsync();
            try
            {
                var draft = await admin.QuestionBank.GetEditorAsync(draftId);
                await admin.QuestionBank.DeleteQuestionAsync(draftId, Find(draft, "A03").Question.Id);

                var after = await admin.QuestionBank.GetEditorAsync(draftId);
                Assert.DoesNotContain(after.Categories.SelectMany(c => c.Questions), q => q.Code == "A03");
                Assert.Null(Find(after, "A04").Question.ParentCode);
                Assert.Null(Find(after, "A05").Question.ParentCode);

                // Deleting a whole category with branched questions inside works too.
                var categoryG = after.Categories.Single(c => c.Code == "ز");
                await admin.QuestionBank.DeleteCategoryAsync(draftId, categoryG.Id);
                Assert.DoesNotContain((await admin.QuestionBank.GetEditorAsync(draftId)).Categories, c => c.Code == "ز");
            }
            finally
            {
                await admin.QuestionBank.DiscardDraftAsync(draftId);
            }
        }

        [LocalDbFact]
        public async Task Weights_are_saved_and_publishing_requires_a_100_percent_total()
        {
            await using var admin = _db.AsAdmin();
            var draftId = await admin.QuestionBank.CreateDraftAsync();
            try
            {
                var draft = await admin.QuestionBank.GetEditorAsync(draftId);
                var category = draft.Categories.Single(c => c.Code == "ج");
                var c05 = category.Questions.Single(q => q.Code == "C05");

                await admin.QuestionBank.SaveCategoryWeightsAsync(draftId, category.Id, new SaveCategoryWeightsRequest(
                    category.BaseWeight + 10,
                    new Dictionary<long, decimal> { [2] = 1.5m },
                    new Dictionary<string, QuestionWeightInput> { ["C05"] = new(4, 3, 2, false) }));

                var saved = await admin.QuestionBank.GetEditorAsync(draftId);
                var savedCategory = saved.Categories.Single(c => c.Code == "ج");
                Assert.Equal(1.5m, savedCategory.Factors[2]);
                var savedC05 = savedCategory.Questions.Single(q => q.Code == "C05");
                Assert.Equal(new[] { 4m, 3m, 2m }, savedC05.Weights);
                Assert.False(savedC05.IsCritical);

                var error = await Assert.ThrowsAsync<BusinessRuleException>(() => admin.QuestionBank.PublishAsync(draftId, null));
                Assert.Contains("100%", error.Message);
            }
            finally
            {
                await admin.QuestionBank.DiscardDraftAsync(draftId);
            }
        }

        [LocalDbFact]
        public async Task Reviewers_cannot_use_the_question_bank_admin()
        {
            await using var reviewer = _db.AsReviewer();
            await Assert.ThrowsAsync<ForbiddenException>(() => reviewer.QuestionBank.GetOverviewAsync());
            await Assert.ThrowsAsync<ForbiddenException>(() => reviewer.QuestionBank.CreateDraftAsync());
        }
    }
}
