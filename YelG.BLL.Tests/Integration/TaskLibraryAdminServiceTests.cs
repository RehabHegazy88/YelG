using Microsoft.EntityFrameworkCore;
using YelG.BLL.Common;
using YelG.BLL.ViewModel;
using YelG.Model.Enums;

namespace YelG.BLL.Tests.Integration
{
    [Collection(ServiceDatabaseCollection.Name)]
    public class TaskLibraryAdminServiceTests
    {
        private readonly ServiceDatabase _db;

        public TaskLibraryAdminServiceTests(ServiceDatabase db) => _db = db;

        private static string NewCode(string prefix) => prefix + "-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        private static SaveTemplateRequest Template(string code, RoleHoursInput hours, TaskFrequency frequency = TaskFrequency.Monthly,
            string? driver = null) => new(
            code, "مهمة اختبار " + code, "Internal Audit", "دليل الإنجاز", "مهمة", TaskOwner.YelgTeam, null,
            hours, driver, driver is null ? 1 : 5, driver is null ? 1 : 4, frequency, TaskPriority.Medium,
            DefaultIncluded: false, IsActive: true, BasisStatus.Approved, "Timesheet تجريبي");

        private static SaveRuleRequest Rule(string question, RuleOperator op, string? value, RuleEffectType effect, long? templateId,
            RoleHoursInput? delta = null, string? recommendation = null) =>
            new(null, question, op, value, effect, templateId, delta, recommendation, null, TaskPriority.High, true);

        /// <summary>A new evaluation of reviewer 1 with the given answers saved.</summary>
        private async Task<long> EvaluationWithAsync(Dictionary<string, IReadOnlyList<string>> answers)
        {
            long evaluationId;
            await using (var admin = _db.AsAdmin())
            {
                var company = await admin.Companies.CreateAsync(new CreateCompanyRequest("نطاق " + Guid.NewGuid().ToString("N")[..8], 1, new[] { _db.ReviewerId }));
                evaluationId = await admin.Context.Evaluations
                    .Where(e => e.CompanyId == company.CompanyId && e.ReviewerId == _db.ReviewerId).Select(e => e.Id).SingleAsync();
            }
            await using var reviewer = _db.AsReviewer();
            var opened = await reviewer.Evaluations.GetAsync(evaluationId);
            await reviewer.Evaluations.SaveAsync(evaluationId, new SaveEvaluationRequest(answers, null, null, opened.RowVersion));
            return evaluationId;
        }

        [LocalDbFact]
        public async Task New_task_and_rules_shape_the_scope_of_an_evaluation()
        {
            long templateId;
            await using (var admin = _db.AsAdmin())
            {
                templateId = await admin.TaskLibrary.SaveTemplateAsync(null, Template(NewCode("T"), new RoleHoursInput(2, 1, 0, 0), driver: "A08"));
                await admin.TaskLibrary.SaveRuleAsync(null, Rule("C09", RuleOperator.In, "نعم | غير منطبق", RuleEffectType.AddProcedure, templateId));
                await admin.TaskLibrary.SaveRuleAsync(null, Rule("C09", RuleOperator.Equals, "نعم", RuleEffectType.ExpandTask, templateId, new RoleHoursInput(0, 1.5m, 0, 0)));
            }

            var evaluationId = await EvaluationWithAsync(new()
            {
                ["C09"] = new[] { "نعم" },
                ["A08"] = new[] { "١٢" }  // 12 employees, 5 per step → factor 3
            });

            await using var check = _db.AsAdmin();
            var scope = (await check.Scope.GenerateAsync(evaluationId))!;
            var task = scope.Tasks.Single(t => t.Template.Id == templateId);
            Assert.Equal(3, task.VolumeFactor);
            Assert.Equal(6m, task.RoleHours.Analyst);
            Assert.Equal(4.5m, task.RoleHours.TeamLead);     // 1 × 3 + 1.5 extra
            Assert.Equal(TaskPriority.High, task.Priority);
            Assert.Equal(2, task.Sources.Count);

            var library = await check.TaskLibrary.GetLibraryAsync();
            Assert.Equal(2, library.Templates.Single(t => t.Id == templateId).RuleCount);
            Assert.Equal("نعم|غير منطبق", library.Rules.First(r => r.TaskTemplateId == templateId && r.Operator == RuleOperator.In).Value);
        }

        [LocalDbFact]
        public async Task Invalid_tasks_and_rules_are_rejected()
        {
            await using var admin = _db.AsAdmin();
            var templateId = await admin.TaskLibrary.SaveTemplateAsync(null, Template(NewCode("T"), new RoleHoursInput(1, 0, 0, 0)));

            async Task Rejects(Func<Task> action) => await Assert.ThrowsAsync<BusinessRuleException>(action);

            await Rejects(() => admin.TaskLibrary.SaveTemplateAsync(null, Template("bad code!", new RoleHoursInput(1, 0, 0, 0))));
            await Rejects(() => admin.TaskLibrary.SaveTemplateAsync(null, Template(NewCode("T"), new RoleHoursInput(1, 0, 0, 0), driver: "C01"))); // not numeric
            await Rejects(() => admin.TaskLibrary.SaveTemplateAsync(null, Template(NewCode("T"), new RoleHoursInput(-1, 0, 0, 0))));

            await Rejects(() => admin.TaskLibrary.SaveRuleAsync(null, Rule("C05", RuleOperator.Equals, "قيمة غير موجودة", RuleEffectType.AddProcedure, templateId)));
            await Rejects(() => admin.TaskLibrary.SaveRuleAsync(null, Rule("A08", RuleOperator.GreaterThan, "كثير", RuleEffectType.AddProcedure, templateId)));
            await Rejects(() => admin.TaskLibrary.SaveRuleAsync(null, Rule("C05", RuleOperator.Equals, "فصل جزئي", RuleEffectType.AddProcedure, null)));
            await Rejects(() => admin.TaskLibrary.SaveRuleAsync(null, Rule("C05", RuleOperator.Equals, "فصل جزئي", RuleEffectType.Recommendation, null, recommendation: "")));
            await Rejects(() => admin.TaskLibrary.SaveRuleAsync(null, Rule("NOPE", RuleOperator.Answered, null, RuleEffectType.ScoreOnly, null)));

            // Answered needs no value; a recommendation needs no task.
            await admin.TaskLibrary.SaveRuleAsync(null, Rule("A08", RuleOperator.Answered, "ignored", RuleEffectType.Recommendation, null, recommendation: "توصية"));
        }

        [LocalDbFact]
        public async Task Tasks_tracked_in_a_scope_cannot_be_deleted_but_unused_ones_go_with_their_rules()
        {
            await using var admin = _db.AsAdmin();
            var unused = await admin.TaskLibrary.SaveTemplateAsync(null, Template(NewCode("T"), new RoleHoursInput(1, 0, 0, 0)));
            await admin.TaskLibrary.SaveRuleAsync(null, Rule("C09", RuleOperator.Equals, "لا", RuleEffectType.AddProcedure, unused));
            await admin.TaskLibrary.DeleteTemplateAsync(unused);
            Assert.False(await admin.Context.AnswerRules.AnyAsync(r => r.TaskTemplateId == unused));
            Assert.False(await admin.Context.TaskTemplates.AnyAsync(t => t.Id == unused));

            // T-TEST is default-included; once an admin sets its status on a submitted evaluation it is "used".
            var evaluationId = await EvaluationWithAsync(new() { ["C01"] = new[] { "نعم" } });
            await using (var reviewer = _db.AsReviewer()) await reviewer.Evaluations.SubmitAsync(evaluationId, null);
            var testTemplateId = await admin.Context.TaskTemplates.Where(t => t.Code == "T-TEST").Select(t => t.Id).SingleAsync();
            await admin.Scope.UpdateItemAsync(evaluationId, new UpdateScopeItemRequest(testTemplateId, false, ScopeItemStatus.Planned, TaskOwner.YelgTeam, null, null, false));

            await using var again = _db.AsAdmin();
            var error = await Assert.ThrowsAsync<BusinessRuleException>(() => again.TaskLibrary.DeleteTemplateAsync(testTemplateId));
            Assert.Contains("إيقافها", error.Message);
            Assert.True((await again.TaskLibrary.GetLibraryAsync()).Templates.Single(t => t.Id == testTemplateId).IsUsed);
        }

        [LocalDbFact]
        public async Task Price_appears_only_when_rates_are_set_and_every_task_has_hours()
        {
            await using var admin = _db.AsAdmin();
            var templateId = await admin.TaskLibrary.SaveTemplateAsync(null, Template(NewCode("P"), new RoleHoursInput(3, 0, 0, 0)));
            await admin.TaskLibrary.SaveRuleAsync(null, Rule("C09", RuleOperator.Equals, "غير منطبق", RuleEffectType.AddProcedure, templateId));
            var evaluationId = await EvaluationWithAsync(new() { ["C09"] = new[] { "غير منطبق" } });

            try
            {
                var scope = (await admin.Scope.GenerateAsync(evaluationId))!;
                Assert.Null(await admin.Scope.PriceAsync(scope)); // no rates yet

                await admin.TaskLibrary.SavePricingAsync(new SavePricingRequest(new RoleHoursInput(100, 200, 0, 0), 10, BasisStatus.Draft, "تجربة"));
                // T-TEST (2 team-leader hours, as needed) + this task (3 analyst hours, monthly → 36 a year).
                Assert.Equal(new[] { "T-TEST", templateId.ToString() }.Length, scope.Tasks.Count);
                Assert.Equal((36m * 100 + 2m * 200) * 1.10m, await admin.Scope.PriceAsync(scope));

                var withUnestimated = await EvaluationWithAsync(new() { ["C09"] = new[] { "غير منطبق" }, ["C05"] = new[] { "فصل جزئي" } });
                var partial = (await admin.Scope.GenerateAsync(withUnestimated))!;
                Assert.Contains(partial.Tasks, t => t.Hours == 0); // XL-C05 from the Excel library has no hours yet
                Assert.Null(await admin.Scope.PriceAsync(partial));
            }
            finally
            {
                await admin.TaskLibrary.SavePricingAsync(new SavePricingRequest(new RoleHoursInput(0, 0, 0, 0), 0, BasisStatus.Draft, null));
            }
        }

        [LocalDbFact]
        public async Task Reviewers_cannot_edit_the_library()
        {
            await using var reviewer = _db.AsReviewer();
            await Assert.ThrowsAsync<ForbiddenException>(() => reviewer.TaskLibrary.GetLibraryAsync());
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                reviewer.TaskLibrary.SavePricingAsync(new SavePricingRequest(new RoleHoursInput(1, 1, 1, 1), 0, BasisStatus.Draft, null)));
        }
    }
}
