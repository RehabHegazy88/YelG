using YelG.BLL.Scoring;
using YelG.Model.Enums;

namespace YelG.BLL.ViewModel
{
    public sealed record QuestionRef(string Code, string Text);

    public sealed record CategoryProgress(string Code, int Visible, int Answered);

    /// <summary>Compact, JSON-friendly score for screens and AJAX responses.</summary>
    public sealed record ScoreSummary(
        double Confidence, ConfidenceGrade Grade, int Answered, int VisibleCount,
        IReadOnlyList<QuestionRef> Breaches, IReadOnlyList<QuestionRef> Unscored,
        IReadOnlyList<CategoryScore> Categories)
    {
        public static ScoreSummary From(EvaluationScore score) => new(
            score.Confidence, score.Grade, score.Answered, score.VisibleCount,
            score.Breaches.Select(q => new QuestionRef(q.Code, q.Text)).ToList(),
            score.Unscored.Select(q => new QuestionRef(q.Code, q.Text)).ToList(),
            score.Categories);
    }

    /// <summary>Live result of unsaved answers: which questions are visible now and the score.</summary>
    public sealed record EvaluationPreview(
        IReadOnlyList<string> VisibleCodes, IReadOnlyList<CategoryProgress> Progress, ScoreSummary Score);
}
