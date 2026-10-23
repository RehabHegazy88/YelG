using YelG.BLL.Common;
using YelG.BLL.IRepository;
using YelG.BLL.ViewModel;

namespace YelG.BLL.Services.QuestionBank
{
    public sealed class QuestionBankService : IQuestionBankService
    {
        private readonly IQuestionBankRepository _questionBank;

        public QuestionBankService(IQuestionBankRepository questionBank)
        {
            _questionBank = questionBank;
        }

        public async Task<List<ActivityItem>> GetActivitiesAsync(CancellationToken ct = default)
        {
            return (await _questionBank.GetActivitiesAsync(ct)).Select(a => new ActivityItem(a.Id, a.ArabicName)).ToList();
        }

        public async Task<QuestionBankForm> GetFormAsync(long versionId, CancellationToken ct = default)
        {
            var version = await _questionBank.GetVersionAsync(versionId, ct)
                ?? throw new NotFoundException("نسخة بنك الأسئلة المطلوبة غير موجودة.");

            var codeById = version.Categories.SelectMany(c => c.Questions).ToDictionary(q => q.Id, q => q.Code);

            return new QuestionBankForm(version.Id, version.Categories
                .OrderBy(c => c.SortOrder)
                .Select(c => new CategoryForm(c.Code, c.Title, c.Status, c.Questions
                    .Where(q => q.IsActive)
                    .OrderBy(q => q.SortOrder)
                    .Select(q => new QuestionForm(
                        q.Code, q.Text, q.AnswerType, q.Kind, q.IsCritical, q.AllowCustomAnswer,
                        q.ParentQuestionId is long parentId && codeById.TryGetValue(parentId, out var parentCode) ? parentCode : null,
                        q.Options.OrderBy(o => o.SortOrder).Select(o => new OptionForm(o.Text, o.Score is decimal s ? (double)s : null)).ToList()))
                    .ToList()))
                .ToList());
        }
    }
}
