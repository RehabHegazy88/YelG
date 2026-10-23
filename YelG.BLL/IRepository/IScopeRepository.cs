using YelG.Model.Scope;

namespace YelG.BLL.IRepository
{
    public interface IScopeRepository
    {
        Task<List<TaskTemplate>> GetTemplatesAsync(CancellationToken ct = default);

        Task<List<AnswerRule>> GetRulesAsync(CancellationToken ct = default);

        Task<List<ScopeItem>> GetScopeItemsAsync(long evaluationId, CancellationToken ct = default);

        /// <summary>Tracked row for one task of an evaluation, or null when nobody has set it yet.</summary>
        Task<ScopeItem?> GetItemForUpdateAsync(long evaluationId, long templateId, bool isOptionalService, CancellationToken ct = default);

        Task<TaskTemplate?> GetTemplateAsync(long templateId, CancellationToken ct = default);

        void AddItem(ScopeItem item);

        Task<TaskTemplate?> GetTemplateForUpdateAsync(long templateId, CancellationToken ct = default);

        Task<AnswerRule?> GetRuleForUpdateAsync(long ruleId, CancellationToken ct = default);

        /// <summary>Tracked rules that target a template (to delete them with it).</summary>
        Task<List<AnswerRule>> GetRulesForTemplateAsync(long templateId, CancellationToken ct = default);

        /// <summary>True when any evaluation already tracks this task in its scope.</summary>
        Task<bool> IsTemplateUsedAsync(long templateId, CancellationToken ct = default);

        Task<HashSet<long>> GetUsedTemplateIdsAsync(CancellationToken ct = default);

        Task<bool> TemplateCodeExistsAsync(string code, CancellationToken ct = default);

        Task<bool> RuleCodeExistsAsync(string code, CancellationToken ct = default);

        /// <summary>Tracked settings row, created on first use.</summary>
        Task<PricingSettings> GetPricingForUpdateAsync(CancellationToken ct = default);

        void AddTemplate(TaskTemplate template);

        void RemoveTemplate(TaskTemplate template);

        void AddRule(AnswerRule rule);

        void RemoveRule(AnswerRule rule);

        Task<PricingSettings> GetPricingSettingsAsync(CancellationToken ct = default);
    }
}
