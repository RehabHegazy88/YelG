using YelG.BLL.Scoring;
using YelG.Model.Enums;

namespace YelG.BLL.ViewModel
{
    public sealed record EvaluationListItem(
        long Id, long CompanyId, string CompanyName, string ActivityName,
        long ReviewerId, string ReviewerName, EvaluationStatus Status,
        double Confidence, ConfidenceGrade Grade, int Answered, int VisibleCount, int BreachCount,
        ConfidenceGrade? ProfessionalJudgment, DateTimeOffset? UpdatedAt);

    public sealed record EvaluationPermissions(bool CanEdit, bool CanSubmit, bool CanReview, bool CanDelete, string? LockMessage);

    public sealed record EvaluationDetails(
        long Id, long CompanyId, string CompanyName, long ActivityId, string ActivityName,
        long ReviewerId, string ReviewerName, long QuestionBankVersionId, EvaluationStatus Status,
        ConfidenceGrade? ProfessionalJudgment, string? Note,
        IReadOnlyDictionary<string, IReadOnlyList<string>> Answers,
        string RowVersion, EvaluationPermissions Permissions, EvaluationScore Score,
        DateTimeOffset? SubmittedAt, long? ReviewedById, DateTimeOffset? ReviewedAt);

    /// <summary>
    /// The full set of answers (question code → values). Codes left out are cleared,
    /// like the old full-state save. RowVersion is the value returned when the evaluation was opened.
    /// </summary>
    public sealed record SaveEvaluationRequest(
        IReadOnlyDictionary<string, IReadOnlyList<string>> Answers,
        ConfidenceGrade? ProfessionalJudgment,
        string? Note,
        string? RowVersion);
}
