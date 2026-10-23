using Microsoft.EntityFrameworkCore;
using YelG.BLL.Common;
using YelG.BLL.ViewModel;
using YelG.Model.Enums;

namespace YelG.BLL.Tests.Integration
{
    [Collection(ServiceDatabaseCollection.Name)]
    public class EvaluationServiceTests
    {
        private const long ManufacturingActivityId = 2;
        private readonly ServiceDatabase _db;

        public EvaluationServiceTests(ServiceDatabase db) => _db = db;

        private static string NewName() => "شركة اختبار " + Guid.NewGuid().ToString("N")[..8];

        private static Dictionary<string, IReadOnlyList<string>> Answers(params (string Code, string[] Values)[] values) =>
            values.ToDictionary(v => v.Code, v => (IReadOnlyList<string>)v.Values);

        /// <summary>Admin assigns a new company to reviewer 1; returns reviewer 1's evaluation id.</summary>
        private async Task<long> AssignToReviewerAsync()
        {
            await using var admin = _db.AsAdmin();
            var result = await admin.Companies.CreateAsync(new CreateCompanyRequest(NewName(), ManufacturingActivityId, new[] { _db.ReviewerId }));
            return await admin.Context.Evaluations
                .Where(e => e.CompanyId == result.CompanyId && e.ReviewerId == _db.ReviewerId)
                .Select(e => e.Id)
                .SingleAsync();
        }

        [LocalDbFact]
        public async Task Admin_creates_company_once_with_an_evaluation_per_active_reviewer()
        {
            var name = NewName();
            await using var admin = _db.AsAdmin();

            var result = await admin.Companies.CreateAsync(
                new CreateCompanyRequest(name, ManufacturingActivityId, new[] { _db.ReviewerId, _db.Reviewer2Id, _db.InactiveId }));

            Assert.Equal(new[] { _db.ReviewerId, _db.Reviewer2Id, _db.AdminId }.OrderBy(x => x), result.AddedUserIds.OrderBy(x => x));
            Assert.Equal(_db.InactiveId, Assert.Single(result.Skipped).UserId);

            var duplicate = await Assert.ThrowsAsync<BusinessRuleException>(() =>
                admin.Companies.CreateAsync(new CreateCompanyRequest("  " + name + "  ", ManufacturingActivityId, Array.Empty<long>())));
            Assert.Contains("موجودة بالفعل", duplicate.Message);
        }

        [LocalDbFact]
        public async Task Regular_user_creates_own_company_and_may_delete_the_draft()
        {
            await using var reviewer = _db.AsReviewer();
            var result = await reviewer.Companies.CreateAsync(
                new CreateCompanyRequest(NewName(), ManufacturingActivityId, new[] { _db.Reviewer2Id }));

            var evaluationId = Assert.Single(await reviewer.Context.Evaluations.Where(e => e.CompanyId == result.CompanyId).ToListAsync()).Id;
            Assert.Equal(new[] { _db.ReviewerId }, result.AddedUserIds);

            var details = await reviewer.Evaluations.GetAsync(evaluationId);
            Assert.True(details.Permissions.CanDelete);

            await reviewer.Evaluations.DeleteAsync(evaluationId);
            Assert.False(await reviewer.Context.Companies.AnyAsync(c => c.Id == result.CompanyId));
        }

        [LocalDbFact]
        public async Task Reviewer_saves_valid_answers_and_gets_the_live_score()
        {
            var evaluationId = await AssignToReviewerAsync();
            await using var reviewer = _db.AsReviewer();
            var opened = await reviewer.Evaluations.GetAsync(evaluationId);
            Assert.False(opened.Permissions.CanDelete); // assigned by the admin

            var saved = await reviewer.Evaluations.SaveAsync(evaluationId, new SaveEvaluationRequest(
                Answers(("C05", new[] { "فصل كامل" }), ("A02", new[] { "تصنيع", "قطاع جديد" }), ("A10", new[] { "١٢٬٥٠٠٬٠٠٠" }), ("B09", new[] { "2026-01-15" })),
                ConfidenceGrade.High, "  ملاحظة  ", opened.RowVersion));

            Assert.Equal(4, saved.Score.Answered);
            Assert.Equal(new[] { "تصنيع", "قطاع جديد" }, saved.Answers["A02"]);
            Assert.Equal("ملاحظة", saved.Note);
            Assert.Equal(ConfidenceGrade.High, saved.ProfessionalJudgment);
            Assert.Contains(saved.Score.Unscored, q => q.Code == "A10"); // numeric with no bands yet
            Assert.NotEqual(opened.RowVersion, saved.RowVersion);
        }

        [LocalDbFact]
        public async Task Invalid_answers_are_rejected()
        {
            var evaluationId = await AssignToReviewerAsync();
            await using var reviewer = _db.AsReviewer();
            var rowVersion = (await reviewer.Evaluations.GetAsync(evaluationId)).RowVersion;

            async Task Rejects(string code, params string[] values) =>
                await Assert.ThrowsAsync<BusinessRuleException>(() => reviewer.Evaluations.SaveAsync(evaluationId,
                    new SaveEvaluationRequest(Answers((code, values)), null, null, rowVersion)));

            await Rejects("C05", "قيمة غير موجودة");          // not an option
            await Rejects("C01", "نعم", "لا");                 // single answer, two values
            await Rejects("C08", "قيمة مخصصة");               // multi without custom answers
            await Rejects("A08", "لا أعرف");                   // number
            await Rejects("B09", "15/01/2026");                // date format
            await Rejects("B01", "مراجعة دائمة");             // cancelled question
        }

        [LocalDbFact]
        public async Task Workflow_submit_review_approve()
        {
            var evaluationId = await AssignToReviewerAsync();

            await using (var reviewer = _db.AsReviewer())
            {
                var opened = await reviewer.Evaluations.GetAsync(evaluationId);
                await reviewer.Evaluations.SaveAsync(evaluationId,
                    new SaveEvaluationRequest(Answers(("C01", new[] { "نعم" })), null, null, opened.RowVersion));
            }

            await using (var admin = _db.AsAdmin())
            {
                var draft = await admin.Evaluations.GetAsync(evaluationId);
                Assert.False(draft.Permissions.CanEdit);
                Assert.StartsWith("عرض فقط", draft.Permissions.LockMessage);
                await Assert.ThrowsAsync<BusinessRuleException>(() => admin.Evaluations.ApproveAsync(evaluationId, null));
                await Assert.ThrowsAsync<BusinessRuleException>(() => admin.Evaluations.SaveAsync(evaluationId,
                    new SaveEvaluationRequest(Answers(("C01", new[] { "لا" })), null, null, draft.RowVersion)));
            }

            await using (var reviewer = _db.AsReviewer())
            {
                var submitted = await reviewer.Evaluations.SubmitAsync(evaluationId, null);
                Assert.Equal(EvaluationStatus.Submitted, submitted.Status);
                await Assert.ThrowsAsync<BusinessRuleException>(() => reviewer.Evaluations.SaveAsync(evaluationId,
                    new SaveEvaluationRequest(Answers(("C01", new[] { "لا" })), null, null, submitted.RowVersion)));
            }

            await using (var admin = _db.AsAdmin())
            {
                var submitted = await admin.Evaluations.GetAsync(evaluationId);
                Assert.True(submitted.Permissions.CanEdit && submitted.Permissions.CanReview);
                var edited = await admin.Evaluations.SaveAsync(evaluationId,
                    new SaveEvaluationRequest(Answers(("C01", new[] { "نعم" }), ("C04", new[] { "نعم" })), null, null, submitted.RowVersion));
                Assert.Equal(EvaluationStatus.Submitted, edited.Status);

                var approved = await admin.Evaluations.ApproveAsync(evaluationId, edited.RowVersion);
                Assert.Equal(EvaluationStatus.Approved, approved.Status);
                Assert.Equal(_db.AdminId, approved.ReviewedById);
                Assert.False(approved.Permissions.CanEdit);
            }

            await using var log = _db.AsAdmin();
            var actions = await log.Context.ActivityLogs.Where(l => l.Details!.Contains("اختبار")).Select(l => l.Action).ToListAsync();
            Assert.Contains("EVALUATION_SUBMITTED", actions);
            Assert.Contains("ADMIN_STATE_UPDATE", actions);
            Assert.Contains("EVALUATION_APPROVED", actions);
        }

        [LocalDbFact]
        public async Task Returned_evaluation_goes_back_to_draft_when_the_reviewer_saves()
        {
            var evaluationId = await AssignToReviewerAsync();
            await using (var reviewer = _db.AsReviewer()) await reviewer.Evaluations.SubmitAsync(evaluationId, null);
            await using (var admin = _db.AsAdmin()) await admin.Evaluations.ReturnAsync(evaluationId, null);

            await using var again = _db.AsReviewer();
            var returned = await again.Evaluations.GetAsync(evaluationId);
            Assert.Equal(EvaluationStatus.Returned, returned.Status);

            var saved = await again.Evaluations.SaveAsync(evaluationId,
                new SaveEvaluationRequest(Answers(("C01", new[] { "لا" })), null, null, returned.RowVersion));
            Assert.Equal(EvaluationStatus.Draft, saved.Status);
        }

        [LocalDbFact]
        public async Task Second_save_with_a_stale_row_version_is_rejected()
        {
            var evaluationId = await AssignToReviewerAsync();
            string staleVersion;
            await using (var first = _db.AsReviewer())
            {
                staleVersion = (await first.Evaluations.GetAsync(evaluationId)).RowVersion;
                await first.Evaluations.SaveAsync(evaluationId,
                    new SaveEvaluationRequest(Answers(("C01", new[] { "نعم" })), null, null, staleVersion));
            }

            await using var second = _db.AsReviewer();
            await Assert.ThrowsAsync<ConcurrencyException>(() => second.Evaluations.SaveAsync(evaluationId,
                new SaveEvaluationRequest(Answers(("C01", new[] { "لا" })), null, null, staleVersion)));
        }

        [LocalDbFact]
        public async Task Other_reviewers_cannot_open_an_evaluation()
        {
            var evaluationId = await AssignToReviewerAsync();
            await using var other = _db.AsReviewer2();

            await Assert.ThrowsAsync<ForbiddenException>(() => other.Evaluations.GetAsync(evaluationId));
            await Assert.ThrowsAsync<ForbiddenException>(() => other.Scope.GenerateAsync(evaluationId));
        }

        [LocalDbFact]
        public async Task Reviewers_who_started_cannot_be_removed()
        {
            long companyId;
            await using (var admin = _db.AsAdmin())
            {
                companyId = (await admin.Companies.CreateAsync(new CreateCompanyRequest(NewName(), ManufacturingActivityId,
                    new[] { _db.ReviewerId, _db.Reviewer2Id }))).CompanyId;
            }

            await using (var reviewer = _db.AsReviewer())
            {
                var mine = (await reviewer.Evaluations.ListMineAsync()).Single(e => e.CompanyId == companyId);
                var opened = await reviewer.Evaluations.GetAsync(mine.Id);
                await reviewer.Evaluations.SaveAsync(mine.Id,
                    new SaveEvaluationRequest(Answers(("C01", new[] { "نعم" })), null, null, opened.RowVersion));
            }

            await using var again = _db.AsAdmin();
            var result = await again.Companies.SetReviewersAsync(companyId, Array.Empty<long>());

            Assert.Equal(new[] { _db.ReviewerId }, result.LockedUserIds);
            Assert.Equal(new[] { _db.Reviewer2Id }, result.RemovedUserIds);
            var reviewers = (await again.Companies.ListAsync()).Single(c => c.Id == companyId).Reviewers.Select(r => r.UserId);
            Assert.Equal(new[] { _db.AdminId, _db.ReviewerId }.OrderBy(x => x), reviewers.OrderBy(x => x));
        }

        [LocalDbFact]
        public async Task Admin_updates_scope_items_only_after_submission()
        {
            var evaluationId = await AssignToReviewerAsync();
            await using (var admin = _db.AsAdmin())
            {
                var templateId = await admin.Context.TaskTemplates.Where(t => t.Code == "T-TEST").Select(t => t.Id).SingleAsync();
                var request = new UpdateScopeItemRequest(templateId, false, ScopeItemStatus.InProgress, TaskOwner.Client,
                    new DateOnly(2026, 11, 30), "دليل", false);

                await Assert.ThrowsAsync<BusinessRuleException>(() => admin.Scope.UpdateItemAsync(evaluationId, request));
            }

            await using (var reviewer = _db.AsReviewer()) await reviewer.Evaluations.SubmitAsync(evaluationId, null);

            await using var after = _db.AsAdmin();
            var testTemplateId = await after.Context.TaskTemplates.Where(t => t.Code == "T-TEST").Select(t => t.Id).SingleAsync();
            await after.Scope.UpdateItemAsync(evaluationId, new UpdateScopeItemRequest(testTemplateId, false,
                ScopeItemStatus.InProgress, TaskOwner.Client, new DateOnly(2026, 11, 30), "دليل", false));

            var task = (await after.Scope.GenerateAsync(evaluationId))!.Tasks.Single(t => t.Template.Code == "T-TEST");
            Assert.Equal(ScopeItemStatus.InProgress, task.State.Status);
            Assert.Equal(TaskOwner.Client, task.State.Owner);
            Assert.Equal(new DateOnly(2026, 11, 30), task.State.DueDate);
            Assert.Equal(2m, task.Hours);
        }
    }
}
