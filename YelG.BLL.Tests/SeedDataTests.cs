using YelG.DAL.Seeding;
using YelG.Model.Enums;

namespace YelG.BLL.Tests
{
    public class SeedDataTests
    {
        [Fact]
        public void Question_bank_has_the_patched_Apps_Script_content()
        {
            var version = SeedData.LoadQuestionBank();
            var questions = version.Categories.SelectMany(c => c.Questions).ToList();

            Assert.Equal(13, version.Categories.Count);
            Assert.Equal(154, questions.Count);
            Assert.Equal(12, questions.Count(q => q.IsCritical));
            Assert.Equal(QuestionBankVersionStatus.Published, version.Status);

            // B01 / B03 / B07 were cancelled; B10, B11, I09, I10, K09, K10 were added.
            Assert.DoesNotContain(questions, q => q.Code is "B01" or "B03" or "B07");
            Assert.All(new[] { "B10", "B11", "I09", "I10", "K09", "K10" }, code => Assert.Contains(questions, q => q.Code == code));

            var a02 = questions.Single(q => q.Code == "A02");
            Assert.Equal(AnswerType.Multi, a02.AnswerType);
            Assert.True(a02.AllowCustomAnswer);
            Assert.Null(a02.Options.Single(o => o.Text == "F&B – الأغذية والمشروبات").Score);
        }

        [Fact]
        public void Every_branch_points_to_a_question_in_the_same_version()
        {
            var questions = SeedData.LoadQuestionBank().Categories.SelectMany(c => c.Questions).ToList();

            foreach (var child in questions.Where(q => q.ParentQuestion is not null))
            {
                Assert.Contains(child.ParentQuestion!, questions);
                Assert.NotNull(child.ConditionOperator);
            }
        }

        [Fact]
        public void Category_weights_are_equal_so_scores_match_the_old_default()
        {
            var weights = SeedData.LoadQuestionBank().Categories.Select(c => c.BaseWeight).Distinct().ToList();

            Assert.Single(weights);
        }

        [Fact]
        public void Task_library_links_every_rule_to_an_existing_task()
        {
            var (templates, rules) = SeedData.LoadTaskLibrary();

            Assert.Equal(72, templates.Count);
            Assert.Equal(72, rules.Count);
            Assert.All(rules, r => Assert.Contains(r.TaskTemplate!, templates));
            Assert.All(templates, t => Assert.Equal(0m, t.RoleHours.Total)); // hours still await YELG approval
        }
    }
}
