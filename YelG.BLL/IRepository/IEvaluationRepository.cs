using YelG.Model.Evaluations;

namespace YelG.BLL.IRepository
{
    public interface IEvaluationRepository
    {
        /// <summary>Read-only evaluation with its company and answers (each answer with its question).</summary>
        Task<Evaluation?> GetWithAnswersAsync(long evaluationId, CancellationToken ct = default);

        /// <summary>Tracked evaluation with company and answers, for saving and status changes.</summary>
        Task<Evaluation?> GetForUpdateAsync(long evaluationId, CancellationToken ct = default);

        /// <summary>Read-only list with company, activity, reviewer and answers. Null reviewer = everyone's.</summary>
        Task<List<Evaluation>> ListAsync(long? reviewerId, CancellationToken ct = default);

        void Add(Evaluation evaluation);

        void Remove(Evaluation evaluation);

        /// <summary>
        /// Marks the evaluation modified and checks it against the row version the user opened,
        /// so a save of answers only also bumps RowVersion and detects concurrent edits.
        /// </summary>
        void MarkChanged(Evaluation evaluation, byte[]? openedRowVersion);
    }
}
