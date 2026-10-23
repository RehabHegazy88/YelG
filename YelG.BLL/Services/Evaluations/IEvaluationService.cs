using YelG.BLL.ViewModel;

namespace YelG.BLL.Services.Evaluations
{
    public interface IEvaluationService
    {
        /// <summary>The signed-in user's evaluations with their current score.</summary>
        Task<List<EvaluationListItem>> ListMineAsync(CancellationToken ct = default);

        /// <summary>Admin: every evaluation of every reviewer, submitted first.</summary>
        Task<List<EvaluationListItem>> ListAllAsync(CancellationToken ct = default);

        /// <summary>Owner or admin. Includes answers, live score and what the user may do.</summary>
        Task<EvaluationDetails> GetAsync(long evaluationId, CancellationToken ct = default);

        /// <summary>
        /// Owner while draft/returned (a returned evaluation goes back to draft), admin while submitted.
        /// </summary>
        Task<EvaluationDetails> SaveAsync(long evaluationId, SaveEvaluationRequest request, CancellationToken ct = default);

        /// <summary>
        /// Owner or admin: visible questions, progress per category and score for answers the user
        /// is still typing. Nothing is saved; values that are not valid options simply do not score.
        /// </summary>
        Task<EvaluationPreview> PreviewAsync(long evaluationId, IReadOnlyDictionary<string, IReadOnlyList<string>> answers,
            CancellationToken ct = default);

        Task<EvaluationDetails> SubmitAsync(long evaluationId, string? rowVersion, CancellationToken ct = default);

        Task<EvaluationDetails> ApproveAsync(long evaluationId, string? rowVersion, CancellationToken ct = default);

        Task<EvaluationDetails> ReturnAsync(long evaluationId, string? rowVersion, CancellationToken ct = default);

        /// <summary>Owner of a self-created draft only; the company goes too when nobody else evaluates it.</summary>
        Task DeleteAsync(long evaluationId, CancellationToken ct = default);
    }
}
