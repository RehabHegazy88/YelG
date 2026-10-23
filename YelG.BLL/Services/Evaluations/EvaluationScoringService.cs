using YelG.BLL.IRepository;
using YelG.BLL.Scoring;

namespace YelG.BLL.Services.Evaluations
{
    public sealed class EvaluationScoringService : IEvaluationScoringService
    {
        private readonly IEvaluationRepository _evaluations;
        private readonly IQuestionBankRepository _questionBank;

        public EvaluationScoringService(IEvaluationRepository evaluations, IQuestionBankRepository questionBank)
        {
            _evaluations = evaluations;
            _questionBank = questionBank;
        }

        public async Task<EvaluationScore?> ScoreAsync(long evaluationId, CancellationToken ct = default)
        {
            var evaluation = await _evaluations.GetWithAnswersAsync(evaluationId, ct);
            if (evaluation is null) return null;

            return await ScoreAsync(evaluation.QuestionBankVersionId, evaluation.Company.ActivityId,
                evaluation.Answers.ToAnswerSet(), ct);
        }

        public async Task<EvaluationScore> ScoreAsync(long questionBankVersionId, long activityId, AnswerSet answers, CancellationToken ct = default)
        {
            var version = await _questionBank.GetVersionAsync(questionBankVersionId, ct)
                ?? throw new InvalidOperationException("نسخة بنك الأسئلة المطلوبة غير موجودة.");

            return ScoringEngine.Score(version.ToScoringModel(), activityId, answers);
        }
    }
}
