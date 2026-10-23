using YelG.Model.Enums;
using YelG.Model.Evaluations;

namespace YelG.BLL.Tests
{
    public class EvaluationWorkflowTests
    {
        private const long Reviewer = 10, Admin = 20, SomeoneElse = 30;

        private static readonly DateTimeOffset Now = new(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);

        private static Evaluation NewEvaluation() => new() { ReviewerId = Reviewer };

        [Fact]
        public void Reviewer_submits_then_admin_approves()
        {
            var evaluation = NewEvaluation();

            evaluation.Submit(Reviewer, Now);
            Assert.Equal(EvaluationStatus.Submitted, evaluation.Status);
            Assert.False(evaluation.CanReviewerEdit);
            Assert.True(evaluation.CanAdminEdit);

            evaluation.Approve(Admin, Now);
            Assert.Equal(EvaluationStatus.Approved, evaluation.Status);
            Assert.Equal(Admin, evaluation.ReviewedById);
            Assert.False(evaluation.CanReviewerEdit);
            Assert.False(evaluation.CanAdminEdit);
        }

        [Fact]
        public void Admin_cannot_act_before_the_reviewer_submits()
        {
            var evaluation = NewEvaluation();

            Assert.Throws<InvalidOperationException>(() => evaluation.Approve(Admin, Now));
            Assert.Throws<InvalidOperationException>(() => evaluation.Return(Admin, Now));
        }

        [Fact]
        public void Only_the_owner_can_submit()
        {
            Assert.Throws<InvalidOperationException>(() => NewEvaluation().Submit(SomeoneElse, Now));
        }

        [Fact]
        public void Returned_evaluation_goes_back_to_draft_when_the_reviewer_edits_it()
        {
            var evaluation = NewEvaluation();
            evaluation.Submit(Reviewer, Now);
            evaluation.Return(Admin, Now);

            Assert.True(evaluation.CanReviewerEdit);
            evaluation.MarkEditedByReviewer();

            Assert.Equal(EvaluationStatus.Draft, evaluation.Status);
        }

        [Fact]
        public void Reviewer_cannot_edit_a_submitted_evaluation()
        {
            var evaluation = NewEvaluation();
            evaluation.Submit(Reviewer, Now);

            Assert.Throws<InvalidOperationException>(() => evaluation.MarkEditedByReviewer());
        }
    }
}
