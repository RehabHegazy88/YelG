using YelG.BLL.ViewModel;

namespace YelG.BLL.Services.QuestionBank
{
    public interface IQuestionBankService
    {
        Task<List<ActivityItem>> GetActivitiesAsync(CancellationToken ct = default);

        /// <summary>Active questions of a version, ordered, for the evaluation form.</summary>
        Task<QuestionBankForm> GetFormAsync(long versionId, CancellationToken ct = default);
    }
}
