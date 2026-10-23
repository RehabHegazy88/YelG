using System.Globalization;
using YelG.BLL.Common;
using YelG.BLL.IRepository;
using YelG.BLL.Scoring;
using YelG.BLL.ViewModel;
using YelG.DAL.Abstractions;
using YelG.Model.Enums;
using YelG.Model.Evaluations;
using YelG.Model.QuestionBank;

namespace YelG.BLL.Services.Evaluations
{
    public sealed class EvaluationService : IEvaluationService
    {
        private const int MaxValueLength = 1000;
        private const int MaxCustomOptionLength = 120;
        private const int MaxValuesPerQuestion = 50;
        private const int MaxNoteLength = 5000;

        private readonly IEvaluationRepository _evaluations;
        private readonly ICompanyRepository _companies;
        private readonly IQuestionBankRepository _questionBank;
        private readonly IActivityLogRepository _log;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly TimeProvider _clock;

        public EvaluationService(
            IEvaluationRepository evaluations,
            ICompanyRepository companies,
            IQuestionBankRepository questionBank,
            IActivityLogRepository log,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            TimeProvider clock)
        {
            _evaluations = evaluations;
            _companies = companies;
            _questionBank = questionBank;
            _log = log;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _clock = clock;
        }

        public async Task<List<EvaluationListItem>> ListMineAsync(CancellationToken ct = default)
        {
            var userId = _currentUser.RequireUserId();
            return await ToListItemsAsync(await _evaluations.ListAsync(userId, ct), ct);
        }

        public async Task<List<EvaluationListItem>> ListAllAsync(CancellationToken ct = default)
        {
            _currentUser.RequireAdmin();
            var items = await ToListItemsAsync(await _evaluations.ListAsync(null, ct), ct);
            return items
                .OrderBy(i => StatusPriority(i.Status))
                .ThenByDescending(i => i.UpdatedAt)
                .ThenBy(i => i.CompanyName)
                .ToList();
        }

        public async Task<EvaluationDetails> GetAsync(long evaluationId, CancellationToken ct = default)
        {
            var evaluation = await _evaluations.GetWithAnswersAsync(evaluationId, ct)
                ?? throw new NotFoundException("التقييم المطلوب غير موجود.");
            EnsureCanView(evaluation);

            var model = (await LoadVersionAsync(evaluation.QuestionBankVersionId, ct)).ToScoringModel();
            var answers = evaluation.Answers.ToAnswerSet();

            return new EvaluationDetails(
                evaluation.Id,
                evaluation.CompanyId,
                evaluation.Company.Name,
                evaluation.Company.ActivityId,
                evaluation.Company.Activity.ArabicName,
                evaluation.ReviewerId,
                evaluation.Reviewer.DisplayName,
                evaluation.QuestionBankVersionId,
                evaluation.Status,
                evaluation.ProfessionalJudgment,
                evaluation.Note,
                answers.ToDictionary(),
                Convert.ToBase64String(evaluation.RowVersion),
                PermissionsFor(evaluation),
                ScoringEngine.Score(model, evaluation.Company.ActivityId, answers),
                evaluation.SubmittedAt,
                evaluation.ReviewedById,
                evaluation.ReviewedAt);
        }

        public async Task<EvaluationDetails> SaveAsync(long evaluationId, SaveEvaluationRequest request, CancellationToken ct = default)
        {
            var userId = _currentUser.RequireUserId();
            var evaluation = await LoadForUpdateAsync(evaluationId, ct);
            var asOwner = evaluation.ReviewerId == userId && evaluation.CanReviewerEdit;
            var asAdmin = _currentUser.IsAdmin && evaluation.CanAdminEdit;
            if (!asOwner && !asAdmin)
                throw new BusinessRuleException(LockMessage(evaluation) ?? "هذا التقييم متاح للعرض فقط.");

            var version = await LoadVersionAsync(evaluation.QuestionBankVersionId, ct);
            var validated = Validate(version, request.Answers);

            if (asOwner) evaluation.MarkEditedByReviewer();
            ReplaceAnswers(evaluation, validated);
            evaluation.ProfessionalJudgment = request.ProfessionalJudgment;
            var note = Text.CleanMultiline(request.Note, MaxNoteLength);
            evaluation.Note = note.Length == 0 ? null : note;

            _evaluations.MarkChanged(evaluation, DecodeRowVersion(request.RowVersion));
            _log.Add(asOwner ? "SAVE_STATE" : "ADMIN_STATE_UPDATE",
                $"{(asOwner ? "حفظ تقييم" : "تعديل Admin لتقييم")}: {evaluation.Company.Name}؛ عدد الإجابات: {validated.Count}");
            await _unitOfWork.SaveChangesAsync(ct);

            return await GetAsync(evaluationId, ct);
        }

        public async Task<EvaluationPreview> PreviewAsync(long evaluationId,
            IReadOnlyDictionary<string, IReadOnlyList<string>> answers, CancellationToken ct = default)
        {
            var evaluation = await _evaluations.GetWithAnswersAsync(evaluationId, ct)
                ?? throw new NotFoundException("التقييم المطلوب غير موجود.");
            EnsureCanView(evaluation);

            var model = (await LoadVersionAsync(evaluation.QuestionBankVersionId, ct)).ToScoringModel();
            var answerSet = new AnswerSet(answers.ToDictionary(p => p.Key, p => p.Value));

            var visible = new List<string>();
            var progress = new List<CategoryProgress>();
            foreach (var category in model.Categories)
            {
                int visibleCount = 0, answeredCount = 0;
                foreach (var question in category.Questions)
                {
                    if (!ScoringEngine.IsVisible(model, question, answerSet)) continue;
                    visible.Add(question.Code);
                    visibleCount++;
                    if (answerSet.IsAnswered(question.Code)) answeredCount++;
                }
                progress.Add(new CategoryProgress(category.Code, visibleCount, answeredCount));
            }

            var score = ScoringEngine.Score(model, evaluation.Company.ActivityId, answerSet);
            return new EvaluationPreview(visible, progress, ScoreSummary.From(score));
        }

        public async Task<EvaluationDetails> SubmitAsync(long evaluationId, string? rowVersion, CancellationToken ct = default)
        {
            var userId = _currentUser.RequireUserId();
            var evaluation = await LoadForUpdateAsync(evaluationId, ct);

            Apply(() => evaluation.Submit(userId, _clock.GetUtcNow()));
            return await CommitStatusAsync(evaluation, rowVersion, "EVALUATION_SUBMITTED", "إرسال تقييم للاعتماد", ct);
        }

        public async Task<EvaluationDetails> ApproveAsync(long evaluationId, string? rowVersion, CancellationToken ct = default)
        {
            var adminId = _currentUser.RequireAdmin();
            var evaluation = await LoadForUpdateAsync(evaluationId, ct);

            Apply(() => evaluation.Approve(adminId, _clock.GetUtcNow()));
            return await CommitStatusAsync(evaluation, rowVersion, "EVALUATION_APPROVED", "اعتماد تقييم", ct);
        }

        public async Task<EvaluationDetails> ReturnAsync(long evaluationId, string? rowVersion, CancellationToken ct = default)
        {
            var adminId = _currentUser.RequireAdmin();
            var evaluation = await LoadForUpdateAsync(evaluationId, ct);

            Apply(() => evaluation.Return(adminId, _clock.GetUtcNow()));
            return await CommitStatusAsync(evaluation, rowVersion, "EVALUATION_RETURNED", "إعادة تقييم للتعديل", ct);
        }

        public async Task DeleteAsync(long evaluationId, CancellationToken ct = default)
        {
            var userId = _currentUser.RequireUserId();
            var evaluation = await LoadForUpdateAsync(evaluationId, ct);
            if (!CanDelete(evaluation, userId))
                throw new BusinessRuleException(evaluation.AssignedById is not null
                    ? "لا يمكن حذف شركة أسندها الـAdmin؛ يمكن استيفاء تقييمها فقط."
                    : "لا يمكن حذف تقييم مُرسل أو معتمد.");

            var company = await _companies.GetForUpdateAsync(evaluation.CompanyId, ct)
                ?? throw new NotFoundException("الشركة المطلوبة غير موجودة.");

            if (company.Evaluations.Count == 1) _companies.Remove(company);
            else _evaluations.Remove(evaluation);

            _log.Add("EVALUATION_DELETED", $"حذف تقييم: {company.Name}");
            await _unitOfWork.SaveChangesAsync(ct);
        }

        private async Task<EvaluationDetails> CommitStatusAsync(Evaluation evaluation, string? rowVersion,
            string action, string label, CancellationToken ct)
        {
            _evaluations.MarkChanged(evaluation, DecodeRowVersion(rowVersion));
            _log.Add(action, $"{label}: {evaluation.Company.Name}؛ صاحب التقييم: {evaluation.Reviewer.DisplayName}");
            await _unitOfWork.SaveChangesAsync(ct);
            return await GetAsync(evaluation.Id, ct);
        }

        /// <summary>
        /// Checks each answer against its question: known and active, one value unless multi-select,
        /// listed options only (custom values only where allowed), valid numbers and dates.
        /// </summary>
        private static Dictionary<long, List<string>> Validate(QuestionBankVersion version,
            IReadOnlyDictionary<string, IReadOnlyList<string>> answers)
        {
            var questions = version.Categories
                .SelectMany(c => c.Questions)
                .Where(q => q.IsActive)
                .ToDictionary(q => q.Code, StringComparer.Ordinal);

            var result = new Dictionary<long, List<string>>();
            foreach (var (code, rawValues) in answers)
            {
                if (!questions.TryGetValue(code, out var question))
                    throw new BusinessRuleException($"سؤال غير معروف في هذه النسخة من بنك الأسئلة: {code}");

                var values = (rawValues ?? Array.Empty<string>())
                    .Select(v => Text.Clean(v, MaxValueLength))
                    .Where(v => v.Length > 0)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                if (values.Count == 0) continue;

                var isMulti = question.AnswerType == AnswerType.Multi;
                if (!isMulti && values.Count > 1)
                    throw new BusinessRuleException($"السؤال {code} يقبل إجابة واحدة فقط.");
                if (values.Count > MaxValuesPerQuestion)
                    throw new BusinessRuleException($"عدد الاختيارات في السؤال {code} أكبر من المسموح.");

                if (question.Options.Count > 0)
                {
                    var options = question.Options.Select(o => o.Text).ToHashSet(StringComparer.Ordinal);
                    foreach (var value in values.Where(v => !options.Contains(v)))
                    {
                        if (!(isMulti && question.AllowCustomAnswer))
                            throw new BusinessRuleException($"الإجابة «{value}» غير متاحة في السؤال {code}.");
                        if (value.Length > MaxCustomOptionLength)
                            throw new BusinessRuleException($"الاختيار المضاف في السؤال {code} طويل جدًا.");
                    }
                }
                else if (question.AnswerType == AnswerType.Number && !NumberParser.TryParse(values[0], out _))
                {
                    throw new BusinessRuleException($"السؤال {code} يحتاج قيمة رقمية.");
                }
                else if (question.AnswerType == AnswerType.Date
                         && !DateOnly.TryParseExact(values[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    throw new BusinessRuleException($"السؤال {code} يحتاج تاريخًا بصيغة yyyy-MM-dd.");
                }

                result[question.Id] = values;
            }
            return result;
        }

        /// <summary>Full replacement: questions missing from the request lose their answer.</summary>
        private void ReplaceAnswers(Evaluation evaluation, Dictionary<long, List<string>> validated)
        {
            foreach (var existing in evaluation.Answers.ToList())
            {
                if (!validated.TryGetValue(existing.QuestionId, out var values))
                {
                    evaluation.Answers.Remove(existing);
                    continue;
                }
                // Only real changes mark the row modified, so LastModifiedDate / ModifiedBy stay meaningful.
                if (!existing.Values.SequenceEqual(values, StringComparer.Ordinal))
                    existing.Values = values;
            }

            foreach (var (questionId, values) in validated)
            {
                if (evaluation.Answers.Any(a => a.QuestionId == questionId)) continue;
                evaluation.Answers.Add(new Answer { QuestionId = questionId, Values = values });
            }
        }

        private async Task<List<EvaluationListItem>> ToListItemsAsync(List<Evaluation> evaluations, CancellationToken ct)
        {
            var models = new Dictionary<long, ScoringModel>();
            var items = new List<EvaluationListItem>(evaluations.Count);

            foreach (var evaluation in evaluations)
            {
                if (!models.TryGetValue(evaluation.QuestionBankVersionId, out var model))
                {
                    model = (await LoadVersionAsync(evaluation.QuestionBankVersionId, ct)).ToScoringModel();
                    models[evaluation.QuestionBankVersionId] = model;
                }
                var score = ScoringEngine.Score(model, evaluation.Company.ActivityId, evaluation.Answers.ToAnswerSet());

                items.Add(new EvaluationListItem(
                    evaluation.Id, evaluation.CompanyId, evaluation.Company.Name, evaluation.Company.Activity.ArabicName,
                    evaluation.ReviewerId, evaluation.Reviewer.DisplayName, evaluation.Status,
                    score.Confidence, score.Grade, score.Answered, score.VisibleCount, score.Breaches.Count,
                    evaluation.ProfessionalJudgment, evaluation.LastModifiedDate.AsUtc()));
            }
            return items;
        }

        private EvaluationPermissions PermissionsFor(Evaluation evaluation)
        {
            var userId = _currentUser.UserId;
            var isOwner = evaluation.ReviewerId == userId;
            var isAdmin = _currentUser.IsAdmin;

            return new EvaluationPermissions(
                CanEdit: (isOwner && evaluation.CanReviewerEdit) || (isAdmin && evaluation.CanAdminEdit),
                CanSubmit: isOwner && evaluation.CanReviewerEdit,
                CanReview: isAdmin && evaluation.Status == EvaluationStatus.Submitted,
                CanDelete: userId is { } uid && CanDelete(evaluation, uid),
                LockMessage: LockMessage(evaluation));
        }

        /// <summary>The same notices the old screen showed (companyLockMessage).</summary>
        private string? LockMessage(Evaluation evaluation)
        {
            var isOwner = evaluation.ReviewerId == _currentUser.UserId;
            if (!isOwner && _currentUser.IsAdmin)
            {
                return evaluation.Status switch
                {
                    EvaluationStatus.Draft => "عرض فقط — المستخدم ما زال يستوفي التقييم ولم يطلب الاعتماد بعد.",
                    EvaluationStatus.Returned => "عرض فقط — التقييم عند المستخدم لاستكمال التعديلات المطلوبة.",
                    EvaluationStatus.Approved => "عرض فقط — تم اعتماد هذا التقييم.",
                    _ => null
                };
            }
            return evaluation.Status switch
            {
                EvaluationStatus.Submitted => "تم إرسال التقييم للاعتماد؛ لا يمكن للمستخدم تعديله حتى يراجعه الـAdmin.",
                EvaluationStatus.Approved => "تم اعتماد التقييم وأصبح متاحًا للعرض فقط.",
                _ => null
            };
        }

        private static bool CanDelete(Evaluation evaluation, long userId) =>
            evaluation.ReviewerId == userId && evaluation.AssignedById is null && evaluation.CanReviewerEdit;

        private void EnsureCanView(Evaluation evaluation)
        {
            var userId = _currentUser.RequireUserId();
            if (evaluation.ReviewerId != userId && !_currentUser.IsAdmin) throw new ForbiddenException();
        }

        private async Task<Evaluation> LoadForUpdateAsync(long evaluationId, CancellationToken ct)
        {
            var evaluation = await _evaluations.GetForUpdateAsync(evaluationId, ct)
                ?? throw new NotFoundException("التقييم المطلوب غير موجود.");
            EnsureCanView(evaluation);
            return evaluation;
        }

        private async Task<QuestionBankVersion> LoadVersionAsync(long versionId, CancellationToken ct) =>
            await _questionBank.GetVersionAsync(versionId, ct)
            ?? throw new NotFoundException("نسخة بنك الأسئلة المطلوبة غير موجودة.");

        /// <summary>Workflow rules live on the entity and throw InvalidOperationException; show them as business errors.</summary>
        private static void Apply(Action transition)
        {
            try
            {
                transition();
            }
            catch (InvalidOperationException ex)
            {
                throw new BusinessRuleException(ex.Message);
            }
        }

        private static byte[]? DecodeRowVersion(string? rowVersion)
        {
            if (string.IsNullOrWhiteSpace(rowVersion)) return null;
            try
            {
                return Convert.FromBase64String(rowVersion);
            }
            catch (FormatException)
            {
                throw new BusinessRuleException("نسخة التقييم غير صالحة. أعيدي تحميل الصفحة.");
            }
        }

        private static int StatusPriority(EvaluationStatus status) => status switch
        {
            EvaluationStatus.Submitted => 0,
            EvaluationStatus.Returned => 1,
            EvaluationStatus.Draft => 2,
            _ => 3
        };
    }
}
