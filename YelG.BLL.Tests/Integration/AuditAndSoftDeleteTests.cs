using Microsoft.EntityFrameworkCore;
using YelG.BLL.ViewModel;
using YelG.Model.Auth;
using YelG.Model.Enums;

namespace YelG.BLL.Tests.Integration
{
    /// <summary>The Basic base entities: who added / modified a row and when, and soft delete.</summary>
    [Collection(ServiceDatabaseCollection.Name)]
    public class AuditAndSoftDeleteTests
    {
        private const long ManufacturingActivityId = 2;

        private readonly ServiceDatabase _db;

        public AuditAndSoftDeleteTests(ServiceDatabase db) => _db = db;

        private static string NewName() => "تتبع " + Guid.NewGuid().ToString("N")[..8];

        [LocalDbFact]
        public async Task Rows_record_who_added_and_who_modified_them_and_when()
        {
            var before = DateTime.UtcNow.AddSeconds(-5);
            long companyId;
            await using (var admin = _db.AsAdmin())
                companyId = (await admin.Companies.CreateAsync(new CreateCompanyRequest(NewName(), ManufacturingActivityId, new[] { _db.ReviewerId }))).CompanyId;

            await using (var check = _db.AsAdmin())
            {
                var company = await check.Context.Companies.SingleAsync(c => c.Id == companyId);
                Assert.Equal(_db.AdminId, company.AddedBy_PersonID);
                Assert.Null(company.ModifiedBy_PersonID);
                Assert.InRange(company.AddedDate, before, DateTime.UtcNow.AddSeconds(5));
                Assert.Equal(company.AddedDate, company.LastModifiedDate);

                // Rows written without a signed-in user (the seed) belong to the built-in system user.
                Assert.Equal(BuiltInUsers.SystemId, (await check.Context.QuestionBankVersions.FirstAsync()).AddedBy_PersonID);
            }

            await using (var admin = _db.AsAdmin())
                await admin.Companies.UpdateAsync(companyId, new UpdateCompanyRequest(NewName(), ManufacturingActivityId));

            await using var after = _db.AsAdmin();
            var updated = await after.Context.Companies.SingleAsync(c => c.Id == companyId);
            Assert.Equal(_db.AdminId, updated.AddedBy_PersonID);
            Assert.Equal(_db.AdminId, updated.ModifiedBy_PersonID);
            Assert.True(updated.LastModifiedDate > updated.AddedDate);

            // The reviewer's own answers carry the reviewer as the one who added them.
            var evaluationId = await after.Context.Evaluations
                .Where(e => e.CompanyId == companyId && e.ReviewerId == _db.ReviewerId).Select(e => e.Id).SingleAsync();
            await using (var reviewer = _db.AsReviewer())
            {
                var opened = await reviewer.Evaluations.GetAsync(evaluationId);
                await reviewer.Evaluations.SaveAsync(evaluationId, new SaveEvaluationRequest(
                    new Dictionary<string, IReadOnlyList<string>> { ["C09"] = new[] { "نعم" } }, null, null, opened.RowVersion));
            }
            await using var answers = _db.AsAdmin();
            Assert.Equal(_db.ReviewerId, (await answers.Context.Answers.SingleAsync(a => a.EvaluationId == evaluationId)).AddedBy_PersonID);
        }

        [LocalDbFact]
        public async Task Deleting_an_evaluation_only_flags_it_and_the_company_name_can_be_used_again()
        {
            var name = NewName();
            long evaluationId, companyId;
            await using (var reviewer = _db.AsReviewer())
            {
                // A self-created company with only this reviewer: deleting the evaluation deletes the company too.
                companyId = (await reviewer.Companies.CreateAsync(new CreateCompanyRequest(name, ManufacturingActivityId, Array.Empty<long>()))).CompanyId;
                evaluationId = (await reviewer.Evaluations.ListMineAsync()).Single(e => e.CompanyId == companyId).Id;
                await reviewer.Evaluations.DeleteAsync(evaluationId);

                Assert.DoesNotContain(await reviewer.Evaluations.ListMineAsync(), e => e.Id == evaluationId);
            }

            await using (var check = _db.AsAdmin())
            {
                Assert.False(await check.Context.Evaluations.AnyAsync(e => e.Id == evaluationId));   // hidden by the filter
                var evaluation = await check.Context.Evaluations.IgnoreQueryFilters().SingleAsync(e => e.Id == evaluationId);
                var company = await check.Context.Companies.IgnoreQueryFilters().SingleAsync(c => c.Id == companyId);
                Assert.True(evaluation.Deleted);
                Assert.True(company.Deleted);
                Assert.NotNull(company.DeleteDate);
                Assert.Equal(_db.ReviewerId, company.ModifiedBy_PersonID);   // who deleted it
            }

            await using var again = _db.AsReviewer();
            var recreated = await again.Companies.CreateAsync(new CreateCompanyRequest(name, ManufacturingActivityId, Array.Empty<long>()));
            Assert.NotEqual(companyId, recreated.CompanyId);
        }

        [LocalDbFact]
        public async Task Deleting_a_task_flags_it_and_its_rules_and_frees_the_code()
        {
            var code = "SD-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var request = new SaveTemplateRequest(code, "مهمة للحذف", "Internal Audit", null, null, TaskOwner.YelgTeam, null,
                new RoleHoursInput(1, 0, 0, 0), null, 1, 1, TaskFrequency.Monthly, TaskPriority.Low,
                DefaultIncluded: false, IsActive: true, BasisStatus.Draft, null);

            await using var admin = _db.AsAdmin();
            var templateId = await admin.TaskLibrary.SaveTemplateAsync(null, request);
            var ruleId = await admin.TaskLibrary.SaveRuleAsync(null, new SaveRuleRequest(null, "C09", RuleOperator.Equals, "نعم",
                RuleEffectType.AddProcedure, templateId, null, null, null, TaskPriority.Medium, true));
            await admin.TaskLibrary.DeleteTemplateAsync(templateId);

            await using var check = _db.AsAdmin();
            Assert.DoesNotContain((await check.TaskLibrary.GetLibraryAsync()).Templates, t => t.Id == templateId);
            Assert.True((await check.Context.TaskTemplates.IgnoreQueryFilters().SingleAsync(t => t.Id == templateId)).Deleted);
            Assert.True((await check.Context.AnswerRules.IgnoreQueryFilters().SingleAsync(r => r.Id == ruleId)).Deleted);

            Assert.NotEqual(templateId, await check.TaskLibrary.SaveTemplateAsync(null, request));   // same code again
        }
    }
}
