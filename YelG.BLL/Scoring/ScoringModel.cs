using YelG.Model.Enums;

namespace YelG.BLL.Scoring
{
    /// <summary>
    /// Read-only snapshot of one question bank version, shaped for the scoring engine.
    /// Holds active questions only, like activeQs() / byId in the Apps Script front end.
    /// </summary>
    public sealed class ScoringModel
    {
        public ScoringModel(long versionId, IReadOnlyList<ScoringCategory> categories)
        {
            VersionId = versionId;
            Categories = categories;
            QuestionsByCode = categories
                .SelectMany(c => c.Questions)
                .ToDictionary(q => q.Code, StringComparer.Ordinal);
        }

        public long VersionId { get; }
        public IReadOnlyList<ScoringCategory> Categories { get; }
        public IReadOnlyDictionary<string, ScoringQuestion> QuestionsByCode { get; }
    }

    public sealed class ScoringCategory
    {
        public ScoringCategory(string code, string title, double baseWeight,
            IReadOnlyDictionary<long, double> activityFactors, IReadOnlyList<ScoringQuestion> questions)
        {
            Code = code;
            Title = title;
            BaseWeight = baseWeight;
            ActivityFactors = activityFactors;
            Questions = questions;
        }

        public string Code { get; }
        public string Title { get; }
        public double BaseWeight { get; }
        public IReadOnlyDictionary<long, double> ActivityFactors { get; }
        public IReadOnlyList<ScoringQuestion> Questions { get; }

        /// <summary>Missing factor means 1, as in FACTORS[activity][code] defaulting to 1.</summary>
        public double FactorFor(long activityId) =>
            ActivityFactors.TryGetValue(activityId, out var factor) ? factor : 1d;
    }

    public sealed class ScoringQuestion
    {
        public string Code { get; init; } = string.Empty;
        public string Text { get; init; } = string.Empty;
        public string CategoryCode { get; init; } = string.Empty;
        public AnswerType AnswerType { get; init; }
        public QuestionKind Kind { get; init; }
        public bool IsCritical { get; init; }

        /// <summary>Confidence, workflow, pricing — w[0..2].</summary>
        public double[] Weights { get; init; } = new double[3];

        public string? ParentCode { get; init; }
        public ConditionOperator? ConditionOperator { get; init; }
        public string? ConditionValue { get; init; }

        public IReadOnlyList<ScoringOption> Options { get; init; } = Array.Empty<ScoringOption>();
        public IReadOnlyList<ScoringBand> Bands { get; init; } = Array.Empty<ScoringBand>();

        public bool IsMulti => AnswerType == AnswerType.Multi;
        public bool IsNumeric => AnswerType == AnswerType.Number || Kind == QuestionKind.Numeric;
    }

    public sealed record ScoringOption(string Text, double? Score);

    /// <summary>Min &lt;= value &lt; Max → Score; a null bound is open.</summary>
    public sealed record ScoringBand(double? Min, double? Max, double Score)
    {
        public bool Contains(double value) => (Min is null || value >= Min) && (Max is null || value < Max);
    }
}
