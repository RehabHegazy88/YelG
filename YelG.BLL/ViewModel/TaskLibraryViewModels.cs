using YelG.Model.Enums;

namespace YelG.BLL.ViewModel
{
    public sealed record RoleHoursInput(decimal Analyst, decimal TeamLead, decimal Manager, decimal Partner)
    {
        public decimal Total => Analyst + TeamLead + Manager + Partner;
    }

    public sealed record TemplateEditor(
        long Id, string Code, string Title, string? ServiceLine, string? Evidence, string? Purpose, TaskOwner DefaultOwner,
        string? SourceQuestionCode, RoleHoursInput RoleHours, string? DriverQuestionCode, decimal DriverUnit, int MaxMultiplier,
        TaskFrequency Frequency, TaskPriority Priority, bool DefaultIncluded, bool IsActive, BasisStatus BasisStatus,
        string? BasisNote, int RuleCount, bool IsUsed);

    public sealed record RuleEditor(
        long Id, string Code, string QuestionCode, string? QuestionText, RuleOperator Operator, string? Value,
        RuleEffectType EffectType, long? TaskTemplateId, string? TaskCode, string? TaskTitle, RoleHoursInput RoleDelta,
        string? Recommendation, string? ConditionNote, TaskPriority Priority, bool IsActive, bool QuestionMissing);

    /// <summary>A question of the published version, for the rule and volume-driver pickers.</summary>
    public sealed record QuestionChoice(string Code, string Text, bool IsNumeric, bool IsMulti, IReadOnlyList<string> Options);

    public sealed record PricingEditor(RoleHoursInput HourlyRates, decimal MarginPct, BasisStatus MethodologyStatus, string? MethodologyNote);

    public sealed record TaskLibrary(
        IReadOnlyList<TemplateEditor> Templates, IReadOnlyList<RuleEditor> Rules,
        IReadOnlyList<QuestionChoice> Questions, PricingEditor Pricing);

    /// <param name="Code">Required when adding; existing task codes do not change.</param>
    public sealed record SaveTemplateRequest(
        string Code, string Title, string? ServiceLine, string? Evidence, string? Purpose, TaskOwner DefaultOwner,
        string? SourceQuestionCode, RoleHoursInput RoleHours, string? DriverQuestionCode, decimal DriverUnit, int MaxMultiplier,
        TaskFrequency Frequency, TaskPriority Priority, bool DefaultIncluded, bool IsActive, BasisStatus BasisStatus, string? BasisNote);

    /// <param name="Code">Optional when adding (a code is generated); existing rule codes do not change.</param>
    public sealed record SaveRuleRequest(
        string? Code, string QuestionCode, RuleOperator Operator, string? Value, RuleEffectType EffectType,
        long? TaskTemplateId, RoleHoursInput? RoleDelta, string? Recommendation, string? ConditionNote,
        TaskPriority Priority, bool IsActive);

    public sealed record SavePricingRequest(RoleHoursInput HourlyRates, decimal MarginPct, BasisStatus MethodologyStatus, string? MethodologyNote);
}
