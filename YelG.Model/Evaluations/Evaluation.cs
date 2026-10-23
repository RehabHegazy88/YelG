using YelG.Model.Auth;
using YelG.Model.Basic;
using YelG.Model.Enums;
using YelG.Model.QuestionBank;
using YelG.Model.Scope;

namespace YelG.Model.Evaluations
{
    /// <summary>
    /// One reviewer's evaluation of one company. The status workflow lives here, replacing
    /// the snapshot comparisons in enforceUserApprovalRules_ / enforceAdminEvaluationEditRules_.
    /// </summary>
    public class Evaluation : MainBaseEntity<User>
    {
        public long CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        public long ReviewerId { get; set; }
        public User Reviewer { get; set; } = null!;

        public long QuestionBankVersionId { get; set; }
        public QuestionBankVersion QuestionBankVersion { get; set; } = null!;

        public EvaluationStatus Status { get; private set; } = EvaluationStatus.Draft;
        public ConfidenceGrade? ProfessionalJudgment { get; set; }
        public string? Note { get; set; }

        public long? AssignedById { get; set; }
        public DateTimeOffset? AssignedAt { get; set; }
        public DateTimeOffset? SubmittedAt { get; private set; }
        public long? ReviewedById { get; private set; }
        public DateTimeOffset? ReviewedAt { get; private set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public ICollection<ScopeItem> ScopeItems { get; set; } = new List<ScopeItem>();

        /// <summary>The reviewer can edit only while the evaluation is with them.</summary>
        public bool CanReviewerEdit => Status is EvaluationStatus.Draft or EvaluationStatus.Returned;

        /// <summary>An admin can edit answers only after the reviewer submits.</summary>
        public bool CanAdminEdit => Status == EvaluationStatus.Submitted;

        /// <summary>Call when the reviewer changes a returned evaluation; it goes back to draft.</summary>
        public void MarkEditedByReviewer()
        {
            if (!CanReviewerEdit)
                throw new InvalidOperationException("تم إرسال هذا التقييم للاعتماد ولا يمكن تعديله حتى يعيده الـAdmin.");
            if (Status == EvaluationStatus.Returned)
                Status = EvaluationStatus.Draft;
        }

        public void Submit(long userId, DateTimeOffset now)
        {
            if (userId != ReviewerId)
                throw new InvalidOperationException("صاحب التقييم فقط يمكنه إرساله للاعتماد.");
            if (!CanReviewerEdit)
                throw new InvalidOperationException("هذا التقييم غير متاح للإرسال حاليًا.");
            Status = EvaluationStatus.Submitted;
            SubmittedAt = now;
        }

        public void Approve(long adminId, DateTimeOffset now) => Review(EvaluationStatus.Approved, adminId, now);

        public void Return(long adminId, DateTimeOffset now) => Review(EvaluationStatus.Returned, adminId, now);

        private void Review(EvaluationStatus decision, long adminId, DateTimeOffset now)
        {
            if (Status != EvaluationStatus.Submitted)
                throw new InvalidOperationException("لا يمكن اتخاذ إجراء قبل أن يرسل المستخدم التقييم للاعتماد.");
            Status = decision;
            ReviewedById = adminId;
            ReviewedAt = now;
        }

        /// <summary>
        /// A reviewer who has started (non-draft, or any answer/judgment/note) cannot be unassigned.
        /// Mirrors the "locked" check in adminSetCompanyReviewers.
        /// </summary>
        public bool HasStarted =>
            Status != EvaluationStatus.Draft
            || Answers.Any(a => a.Values.Count > 0)
            || ProfessionalJudgment is not null
            || !string.IsNullOrWhiteSpace(Note);
    }
}
