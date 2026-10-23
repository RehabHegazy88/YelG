using YelG.Model.Auth;
using YelG.Model.Basic;
using YelG.Model.Enums;

namespace YelG.Model.Scope
{
    /// <summary>What a given answer does to the scope of work (TASK_RULES).</summary>
    public class AnswerRule : MainBaseEntity<User>
    {
        /// <summary>Business code, e.g. "XL-R009".</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Question code (stable across question bank versions), e.g. "C05".</summary>
        public string QuestionCode { get; set; } = string.Empty;
        public RuleOperator Operator { get; set; }
        public string? Value { get; set; }

        public RuleEffectType EffectType { get; set; } = RuleEffectType.AddProcedure;

        /// <summary>Target task for ExpandTask, AddProcedure and OptionalService.</summary>
        public long? TaskTemplateId { get; set; }
        public TaskTemplate? TaskTemplate { get; set; }

        /// <summary>Extra hours added to the target task when EffectType is ExpandTask.</summary>
        public RoleHours RoleDelta { get; set; } = new();

        public string? Recommendation { get; set; }
        public string? ConditionNote { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public bool IsActive { get; set; } = true;
    }
}
