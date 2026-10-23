using YelG.BLL.Scoring;

namespace YelG.BLL.Services.Evaluations
{
    public interface IEvaluationScoringService
    {
        /// <summary>Scores a saved evaluation against the question bank version it was answered on.</summary>
        Task<EvaluationScore?> ScoreAsync(long evaluationId, CancellationToken ct = default);

        /// <summary>Scores answers that are not saved yet (live score while the reviewer types).</summary>
        Task<EvaluationScore> ScoreAsync(long questionBankVersionId, long activityId, AnswerSet answers, CancellationToken ct = default);
    }
}
