using YelG.BLL.Common;
using YelG.BLL.IRepository;
using YelG.BLL.Scoring;
using YelG.BLL.ViewModel;
using YelG.DAL.Abstractions;
using YelG.Model.Enums;
using YelG.Model.Scope;

namespace YelG.BLL.Services.Scope
{
    public sealed class ScopeService : IScopeService
    {
        private const int MaxEvidenceNote = 2000;

        private readonly IEvaluationRepository _evaluations;
        private readonly IQuestionBankRepository _questionBank;
        private readonly IScopeRepository _scope;
        private readonly IActivityLogRepository _log;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public ScopeService(
            IEvaluationRepository evaluations,
            IQuestionBankRepository questionBank,
            IScopeRepository scope,
            IActivityLogRepository log,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser)
        {
            _evaluations = evaluations;
            _questionBank = questionBank;
            _scope = scope;
            _log = log;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<ScopeResult?> GenerateAsync(long evaluationId, CancellationToken ct = default)
        {
            var userId = _currentUser.RequireUserId();
            var evaluation = await _evaluations.GetWithAnswersAsync(evaluationId, ct);
            if (evaluation is null) return null;
            if (evaluation.ReviewerId != userId && !_currentUser.IsAdmin) throw new ForbiddenException();

            var version = await _questionBank.GetVersionAsync(evaluation.QuestionBankVersionId, ct)
                ?? throw new InvalidOperationException("نسخة بنك الأسئلة المطلوبة غير موجودة.");
            var model = version.ToScoringModel();

            var templates = await _scope.GetTemplatesAsync(ct);
            var rules = await _scope.GetRulesAsync(ct);
            var states = (await _scope.GetScopeItemsAsync(evaluationId, ct))
                .ToDictionary(s => (s.TaskTemplateId, s.IsOptionalService), ScopeItemState.From);

            return ScopeEngine.Generate(model, evaluation.Answers.ToAnswerSet(), templates, rules, states);
        }

        public async Task<decimal?> PriceAsync(ScopeResult scope, CancellationToken ct = default) =>
            ScopeEngine.FirstYearPrice(scope.Tasks, await _scope.GetPricingSettingsAsync(ct));

        public async Task UpdateItemAsync(long evaluationId, UpdateScopeItemRequest request, CancellationToken ct = default)
        {
            var adminId = _currentUser.RequireAdmin();
            var evaluation = await _evaluations.GetWithAnswersAsync(evaluationId, ct)
                ?? throw new NotFoundException("التقييم المطلوب غير موجود.");

            var editable = evaluation.ReviewerId == adminId
                || evaluation.Status is EvaluationStatus.Submitted or EvaluationStatus.Approved;
            if (!editable)
                throw new BusinessRuleException("نطاق العمل متاح للعرض فقط حتى يرسل المستخدم التقييم للاعتماد.");

            var template = await _scope.GetTemplateAsync(request.TaskTemplateId, ct)
                ?? throw new NotFoundException("المهمة المطلوبة غير موجودة.");

            var item = await _scope.GetItemForUpdateAsync(evaluationId, template.Id, request.IsOptionalService, ct);
            if (item is null)
            {
                item = new ScopeItem
                {
                    EvaluationId = evaluationId,
                    TaskTemplateId = template.Id,
                    IsOptionalService = request.IsOptionalService
                };
                _scope.AddItem(item);
            }

            item.Status = request.Status;
            item.Owner = request.Owner;
            item.DueDate = request.DueDate;
            var note = Text.CleanMultiline(request.EvidenceNote, MaxEvidenceNote);
            item.EvidenceNote = note.Length == 0 ? null : note;
            item.OptionalIncluded = request.IsOptionalService && request.OptionalIncluded;

            _log.Add("SCOPE_ITEM_UPDATED", $"تحديث مهمة {template.Code} في نطاق عمل: {evaluation.Company.Name}");
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
