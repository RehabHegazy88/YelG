using YelG.Model.Evaluations;
using YelG.Model.QuestionBank;

namespace YelG.BLL.Scoring
{
    public static class ScoringModelMapper
    {
        /// <summary>
        /// Builds the engine snapshot from a version loaded with Categories → Questions → Options
        /// and Categories → ActivityFactors. Inactive questions are left out, as activeQs() did.
        /// </summary>
        public static ScoringModel ToScoringModel(this QuestionBankVersion version)
        {
            var codeById = version.Categories
                .SelectMany(c => c.Questions)
                .Where(q => q.Id != 0)
                .ToDictionary(q => q.Id, q => q.Code);

            var categories = version.Categories
                .OrderBy(c => c.SortOrder)
                .Select(c => new ScoringCategory(
                    c.Code,
                    c.Title,
                    (double)c.BaseWeight,
                    c.ActivityFactors.ToDictionary(f => f.ActivityId, f => (double)f.Factor),
                    c.Questions
                        .Where(q => q.IsActive)
                        .OrderBy(q => q.SortOrder)
                        .Select(q => new ScoringQuestion
                        {
                            Code = q.Code,
                            Text = q.Text,
                            CategoryCode = c.Code,
                            AnswerType = q.AnswerType,
                            Kind = q.Kind,
                            IsCritical = q.IsCritical,
                            Weights = new[] { (double)q.WeightConfidence, (double)q.WeightWorkflow, (double)q.WeightPricing },
                            ParentCode = q.ParentQuestion?.Code
                                ?? (q.ParentQuestionId is long parentId && codeById.TryGetValue(parentId, out var parentCode) ? parentCode : null),
                            ConditionOperator = q.ConditionOperator,
                            ConditionValue = q.ConditionValue,
                            Options = q.Options
                                .OrderBy(o => o.SortOrder)
                                .Select(o => new ScoringOption(o.Text, o.Score is decimal s ? (double)s : null))
                                .ToList(),
                            Bands = q.ScoreBands
                                .OrderBy(b => b.SortOrder)
                                .Select(b => new ScoringBand(
                                    b.MinValue is decimal min ? (double)min : null,
                                    b.MaxValue is decimal max ? (double)max : null,
                                    (double)b.Score))
                                .ToList()
                        })
                        .ToList()))
                .ToList();

            return new ScoringModel(version.Id, categories);
        }

        /// <summary>Answers loaded with Answer.Question, keyed by question code.</summary>
        public static AnswerSet ToAnswerSet(this IEnumerable<Answer> answers) =>
            AnswerSet.From(answers
                .Where(a => a.Question is not null)
                .Select(a => new KeyValuePair<string, IReadOnlyList<string>>(a.Question.Code, a.Values)));
    }
}
