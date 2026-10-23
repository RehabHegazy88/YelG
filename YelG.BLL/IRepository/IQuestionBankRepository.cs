using YelG.Model.QuestionBank;

namespace YelG.BLL.IRepository
{
    public interface IQuestionBankRepository
    {
        /// <summary>The published version with categories, questions, options and activity factors.</summary>
        Task<QuestionBankVersion?> GetPublishedVersionAsync(CancellationToken ct = default);

        Task<QuestionBankVersion?> GetVersionAsync(long versionId, CancellationToken ct = default);

        /// <summary>Id of the latest published version, without loading its questions.</summary>
        Task<long?> GetPublishedVersionIdAsync(CancellationToken ct = default);

        Task<List<Activity>> GetActivitiesAsync(CancellationToken ct = default);

        /// <summary>Tracked version with its full graph (categories, questions, options, bands, factors).</summary>
        Task<QuestionBankVersion?> GetVersionForUpdateAsync(long versionId, CancellationToken ct = default);

        /// <summary>All versions, newest first, with how many evaluations use each one and their question count.</summary>
        Task<List<(QuestionBankVersion Version, int EvaluationCount, int QuestionCount)>> ListVersionsAsync(CancellationToken ct = default);

        Task<long?> GetDraftVersionIdAsync(CancellationToken ct = default);

        Task<int> GetMaxVersionNoAsync(CancellationToken ct = default);

        /// <summary>Question codes the answer rules depend on (to warn when a draft drops one).</summary>
        Task<List<(string RuleCode, string QuestionCode)>> GetRuleQuestionCodesAsync(CancellationToken ct = default);

        void AddVersion(QuestionBankVersion version);

        void RemoveVersion(QuestionBankVersion version);

        void RemoveCategory(Category category);

        void RemoveQuestion(Question question);
    }
}
