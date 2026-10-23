using YelG.BLL.Scoring;
using YelG.BLL.ViewModel;

namespace YelG.BLL.Services.Scope
{
    public interface IScopeService
    {
        /// <summary>Owner or admin: the scope of work generated from the evaluation's answers and saved task states.</summary>
        Task<ScopeResult?> GenerateAsync(long evaluationId, CancellationToken ct = default);

        /// <summary>
        /// Admin only: owner, due date, status, evidence note of a task, or including an optional service.
        /// Allowed on the admin's own evaluation, or on someone else's once submitted or approved.
        /// </summary>
        Task UpdateItemAsync(long evaluationId, UpdateScopeItemRequest request, CancellationToken ct = default);

        /// <summary>
        /// First-year price of the generated tasks, or null while rates are not set or some task has no hours yet
        /// (a partial price would look final when it is not).
        /// </summary>
        Task<decimal?> PriceAsync(ScopeResult scope, CancellationToken ct = default);
    }
}
