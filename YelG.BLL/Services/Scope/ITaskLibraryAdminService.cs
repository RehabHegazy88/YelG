using YelG.BLL.ViewModel;

namespace YelG.BLL.Services.Scope
{
    /// <summary>
    /// Admin: the YELG task library, the effect of answers on the scope, and role rates.
    /// Not versioned — scope is generated live, so changes show in every evaluation's scope at once.
    /// </summary>
    public interface ITaskLibraryAdminService
    {
        Task<TaskLibrary> GetLibraryAsync(CancellationToken ct = default);

        /// <summary>Adds a task when <paramref name="templateId"/> is null, otherwise edits it.</summary>
        Task<long> SaveTemplateAsync(long? templateId, SaveTemplateRequest request, CancellationToken ct = default);

        /// <summary>
        /// Deletes the task and the rules that target it. A task already tracked in an evaluation's
        /// scope cannot be deleted; deactivate it instead.
        /// </summary>
        Task DeleteTemplateAsync(long templateId, CancellationToken ct = default);

        Task<long> SaveRuleAsync(long? ruleId, SaveRuleRequest request, CancellationToken ct = default);

        Task DeleteRuleAsync(long ruleId, CancellationToken ct = default);

        Task SavePricingAsync(SavePricingRequest request, CancellationToken ct = default);
    }
}
