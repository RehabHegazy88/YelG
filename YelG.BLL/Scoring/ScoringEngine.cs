using YelG.Model.Enums;

namespace YelG.BLL.Scoring
{
    public sealed record CategoryScore(
        string Code,
        string Title,
        double? Confidence,
        double? Workflow,
        double? Pricing,
        double RelativeWeight)
    {
        /// <summary>Contribution of this category to the overall confidence.</summary>
        public double? Contribution => Confidence is null ? null : Confidence * RelativeWeight / 100d;
    }

    public sealed class EvaluationScore
    {
        public IReadOnlyList<CategoryScore> Categories { get; init; } = Array.Empty<CategoryScore>();
        public double Confidence { get; init; }
        public IReadOnlyList<ScoringQuestion> Breaches { get; init; } = Array.Empty<ScoringQuestion>();

        /// <summary>
        /// Answered, visible questions that carry a confidence weight but produced no score: numeric
        /// questions without score bands, or only custom answers. "Not applicable" answers are not listed.
        /// </summary>
        public IReadOnlyList<ScoringQuestion> Unscored { get; init; } = Array.Empty<ScoringQuestion>();
        public ConfidenceGrade Grade { get; init; }
        public int Answered { get; init; }
        public int VisibleCount { get; init; }
    }

    /// <summary>
    /// Port of score(), visible() and valueOf() from the Apps Script front end.
    /// Kept in double precision so results match the JavaScript version exactly.
    /// </summary>
    public static class ScoringEngine
    {
        public const double HighThreshold = 80d;
        public const double MediumThreshold = 60d;
        private const int MaxBranchDepth = 32;

        public static bool IsVisible(ScoringModel model, ScoringQuestion question, AnswerSet answers) =>
            IsVisible(model, question, answers, 0);

        private static bool IsVisible(ScoringModel model, ScoringQuestion question, AnswerSet answers, int depth)
        {
            if (string.IsNullOrEmpty(question.ParentCode)) return true;
            if (depth > MaxBranchDepth) return false;
            if (!model.QuestionsByCode.TryGetValue(question.ParentCode, out var parent)) return false;
            if (!IsVisible(model, parent, answers, depth + 1)) return false;

            var parentValues = answers.ValuesOf(parent.Code);
            if (parentValues.Count == 0) return false;

            var expected = question.ConditionValue ?? string.Empty;
            var matches = parent.IsMulti
                ? parentValues.Contains(expected, StringComparer.Ordinal)
                : string.Equals(parentValues[0], expected, StringComparison.Ordinal);

            return question.ConditionOperator == ConditionOperator.NotEquals ? !matches : matches;
        }

        /// <summary>Score 0..1 of the answer, or null when unanswered / not applicable / unscored.</summary>
        public static double? ValueOf(ScoringQuestion question, AnswerSet answers)
        {
            var values = answers.ValuesOf(question.Code);
            if (values.Count == 0) return null;

            if (question.IsNumeric && question.Options.Count == 0)
            {
                if (question.Bands.Count == 0 || !NumberParser.TryParse(values[0], out var number)) return null;
                foreach (var band in question.Bands)
                    if (band.Contains(number)) return band.Score;
                return null;
            }

            if (question.IsMulti)
            {
                var scores = values
                    .Select(v => FindOption(question, v)?.Score)
                    .Where(s => s.HasValue)
                    .Select(s => s!.Value)
                    .ToList();
                return scores.Count > 0 ? scores.Sum() / scores.Count : null;
            }

            return FindOption(question, values[0])?.Score;
        }

        public static EvaluationScore Score(ScoringModel model, long activityId, AnswerSet answers)
        {
            var totalWeight = model.Categories.Sum(c => c.BaseWeight * c.FactorFor(activityId));
            var categories = new List<CategoryScore>(model.Categories.Count);
            var breaches = new List<ScoringQuestion>();
            var unscored = new List<ScoringQuestion>();
            double confidence = 0;
            int answered = 0, visibleCount = 0;

            foreach (var category in model.Categories)
            {
                var sums = new double[3];
                var maxima = new double[3];

                foreach (var question in category.Questions)
                {
                    if (!IsVisible(model, question, answers)) continue;
                    visibleCount++;
                    if (!answers.IsAnswered(question.Code)) continue;
                    answered++;

                    var value = ValueOf(question, answers);
                    if (value is null)
                    {
                        if (question.Weights[0] > 0 && !IsExplicitlyNotApplicable(question, answers)) unscored.Add(question);
                        continue;
                    }

                    for (var i = 0; i < 3; i++)
                    {
                        sums[i] += value.Value * question.Weights[i];
                        maxima[i] += question.Weights[i];
                    }
                    if (question.IsCritical && value.Value < 0.5) breaches.Add(question);
                }

                double? Percent(int i) => maxima[i] > 0 ? sums[i] / maxima[i] * 100d : null;

                var relative = totalWeight > 0
                    ? category.BaseWeight * category.FactorFor(activityId) / totalWeight * 100d
                    : 0d;
                var categoryConfidence = Percent(0);
                if (categoryConfidence is not null) confidence += categoryConfidence.Value * relative / 100d;

                categories.Add(new CategoryScore(category.Code, category.Title,
                    categoryConfidence, Percent(1), Percent(2), relative));
            }

            return new EvaluationScore
            {
                Categories = categories,
                Confidence = confidence,
                Breaches = breaches,
                Unscored = unscored,
                Grade = GradeOf(confidence, breaches.Count),
                Answered = answered,
                VisibleCount = visibleCount
            };
        }

        /// <summary>Any critical breach forces Low; otherwise thresholds 80 / 60.</summary>
        public static ConfidenceGrade GradeOf(double confidence, int breachCount)
        {
            if (breachCount > 0) return ConfidenceGrade.Low;
            if (confidence >= HighThreshold) return ConfidenceGrade.High;
            if (confidence >= MediumThreshold) return ConfidenceGrade.Medium;
            return ConfidenceGrade.Low;
        }

        /// <summary>The reviewer picked a listed option whose score is null (e.g. "غير منطبق").</summary>
        private static bool IsExplicitlyNotApplicable(ScoringQuestion question, AnswerSet answers) =>
            answers.ValuesOf(question.Code).Any(v => FindOption(question, v) is not null);

        private static ScoringOption? FindOption(ScoringQuestion question, string text)
        {
            foreach (var option in question.Options)
                if (string.Equals(option.Text, text, StringComparison.Ordinal)) return option;
            return null;
        }
    }
}
