using YelG.Model.Enums;

namespace YelG.BLL.ViewModel
{
    public sealed record VersionSummary(
        long Id, int VersionNo, QuestionBankVersionStatus Status, int EvaluationCount,
        int QuestionCount, DateTimeOffset CreatedAt, DateTimeOffset? PublishedAt, string? Notes);

    public sealed record OptionInput(string Text, decimal? Score);

    public sealed record BandInput(decimal? MinValue, decimal? MaxValue, decimal Score);

    public sealed record QuestionEditor(
        long Id, string Code, string Text, AnswerType AnswerType, QuestionKind Kind, QuestionDirection Direction,
        bool IsCritical, bool AllowCustomAnswer, bool IsActive, decimal[] Weights,
        string? ParentCode, ConditionOperator? ConditionOperator, string? ConditionValue,
        IReadOnlyList<OptionInput> Options, IReadOnlyList<BandInput> Bands, int ChildCount);

    public sealed record CategoryEditor(
        long Id, string Code, string Title, string Status, int SortOrder, decimal BaseWeight,
        IReadOnlyDictionary<long, decimal> Factors, IReadOnlyList<QuestionEditor> Questions);

    public sealed record QuestionBankEditor(
        long VersionId, int VersionNo, QuestionBankVersionStatus Status, bool IsEditable,
        IReadOnlyList<CategoryEditor> Categories, IReadOnlyList<ActivityItem> Activities)
    {
        public decimal TotalBaseWeight => Categories.Sum(c => c.BaseWeight);
    }

    public sealed record QuestionBankOverview(IReadOnlyList<VersionSummary> Versions, long? DraftId, long? PublishedId);

    /// <param name="Code">Only used when adding; existing category codes do not change.</param>
    public sealed record SaveCategoryRequest(string Code, string Title, string Status, decimal BaseWeight);

    /// <param name="Code">Required when adding; on edit it must match the question being edited.</param>
    public sealed record SaveQuestionRequest(
        string CategoryCode, string Code, string Text, AnswerType AnswerType, QuestionKind Kind, QuestionDirection Direction,
        bool IsCritical, bool AllowCustomAnswer, bool IsActive, decimal[] Weights,
        string? ParentCode, ConditionOperator? ConditionOperator, string? ConditionValue,
        IReadOnlyList<OptionInput> Options, IReadOnlyList<BandInput> Bands);

    public sealed record QuestionWeightInput(decimal Confidence, decimal Workflow, decimal Pricing, bool IsCritical);

    /// <summary>The weights of one category: base weight, factor per activity, weights per question code.</summary>
    public sealed record SaveCategoryWeightsRequest(
        decimal BaseWeight, IReadOnlyDictionary<long, decimal> Factors, IReadOnlyDictionary<string, QuestionWeightInput> Questions);

    /// <param name="Warnings">Things that did not block publishing but deserve a look (e.g. rules on removed questions).</param>
    public sealed record PublishResult(long VersionId, int VersionNo, IReadOnlyList<string> Warnings);
}
