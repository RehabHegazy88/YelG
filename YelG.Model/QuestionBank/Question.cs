using YelG.Model.Auth;
using YelG.Model.Basic;
using YelG.Model.Enums;

namespace YelG.Model.QuestionBank
{
    public class Question : MainBaseEntity<User>
    {
        public long CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        /// <summary>Stable business code, e.g. "C05". Unique within a version.</summary>
        public string Code { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int SortOrder { get; set; }

        public AnswerType AnswerType { get; set; }
        public QuestionKind Kind { get; set; }
        public QuestionDirection Direction { get; set; }
        public bool IsCritical { get; set; }
        public bool AllowCustomAnswer { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>Weights 0..5 for the three axes (w[0], w[1], w[2] in the old model).</summary>
        public decimal WeightConfidence { get; set; }
        public decimal WeightWorkflow { get; set; }
        public decimal WeightPricing { get; set; }

        // Branching: show this question only when the parent's answer matches the condition.
        public long? ParentQuestionId { get; set; }
        public Question? ParentQuestion { get; set; }
        public ConditionOperator? ConditionOperator { get; set; }
        public string? ConditionValue { get; set; }

        public ICollection<Question> ChildQuestions { get; set; } = new List<Question>();
        public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
        public ICollection<QuestionScoreBand> ScoreBands { get; set; } = new List<QuestionScoreBand>();

        /// <summary>Numeric answers are scored through <see cref="ScoreBands"/> instead of options.</summary>
        public bool IsNumeric => AnswerType == AnswerType.Number || Kind == QuestionKind.Numeric;
    }
}
