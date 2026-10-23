using System.Text.RegularExpressions;
using YelG.BLL.Common;
using YelG.BLL.IRepository;
using YelG.BLL.ViewModel;
using YelG.DAL.Abstractions;
using YelG.Model.Enums;
using YelG.Model.QuestionBank;

namespace YelG.BLL.Services.QuestionBank
{
    public sealed class QuestionBankAdminService : IQuestionBankAdminService
    {
        private const decimal WeightSumTolerance = 0.5m;
        private static readonly Regex QuestionCodePattern = new("^[A-Z0-9_-]{1,16}$", RegexOptions.Compiled);

        private readonly IQuestionBankRepository _questionBank;
        private readonly IActivityLogRepository _log;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly TimeProvider _clock;

        public QuestionBankAdminService(
            IQuestionBankRepository questionBank,
            IActivityLogRepository log,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            TimeProvider clock)
        {
            _questionBank = questionBank;
            _log = log;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _clock = clock;
        }

        // ------------------------------------------------------------------ reading

        public async Task<QuestionBankOverview> GetOverviewAsync(CancellationToken ct = default)
        {
            _currentUser.RequireAdmin();
            var versions = (await _questionBank.ListVersionsAsync(ct))
                .Select(x => new VersionSummary(x.Version.Id, x.Version.VersionNo, x.Version.Status, x.EvaluationCount,
                    x.QuestionCount, x.Version.AddedDate.AsUtc(), x.Version.PublishedAt, x.Version.ReleaseNotes))
                .ToList();

            return new QuestionBankOverview(versions,
                versions.FirstOrDefault(v => v.Status == QuestionBankVersionStatus.Draft)?.Id,
                versions.FirstOrDefault(v => v.Status == QuestionBankVersionStatus.Published)?.Id);
        }

        public async Task<QuestionBankEditor> GetEditorAsync(long versionId, CancellationToken ct = default)
        {
            _currentUser.RequireAdmin();
            var version = await _questionBank.GetVersionAsync(versionId, ct)
                ?? throw new NotFoundException("نسخة بنك الأسئلة المطلوبة غير موجودة.");
            var activities = (await _questionBank.GetActivitiesAsync(ct)).Select(a => new ActivityItem(a.Id, a.ArabicName)).ToList();

            var all = version.Categories.SelectMany(c => c.Questions).ToList();
            var codeById = all.ToDictionary(q => q.Id, q => q.Code);
            var childCount = all.Where(q => q.ParentQuestionId is not null)
                .GroupBy(q => q.ParentQuestionId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            var categories = version.Categories.OrderBy(c => c.SortOrder).Select(c => new CategoryEditor(
                c.Id, c.Code, c.Title, c.Status, c.SortOrder, c.BaseWeight,
                c.ActivityFactors.ToDictionary(f => f.ActivityId, f => f.Factor),
                c.Questions.OrderBy(q => q.SortOrder).Select(q => new QuestionEditor(
                    q.Id, q.Code, q.Text, q.AnswerType, q.Kind, q.Direction, q.IsCritical, q.AllowCustomAnswer, q.IsActive,
                    new[] { q.WeightConfidence, q.WeightWorkflow, q.WeightPricing },
                    q.ParentQuestionId is long p && codeById.TryGetValue(p, out var parentCode) ? parentCode : null,
                    q.ConditionOperator, q.ConditionValue,
                    q.Options.OrderBy(o => o.SortOrder).Select(o => new OptionInput(o.Text, o.Score)).ToList(),
                    q.ScoreBands.OrderBy(b => b.SortOrder).Select(b => new BandInput(b.MinValue, b.MaxValue, b.Score)).ToList(),
                    childCount.TryGetValue(q.Id, out var n) ? n : 0)).ToList())).ToList();

            return new QuestionBankEditor(version.Id, version.VersionNo, version.Status,
                version.Status == QuestionBankVersionStatus.Draft, categories, activities);
        }

        // ------------------------------------------------------------------ draft lifecycle

        public async Task<long> CreateDraftAsync(CancellationToken ct = default)
        {
            _currentUser.RequireAdmin();
            if (await _questionBank.GetDraftVersionIdAsync(ct) is long existing) return existing;

            var publishedId = await _questionBank.GetPublishedVersionIdAsync(ct)
                ?? throw new BusinessRuleException("لا توجد نسخة منشورة لنسخها.");
            var source = await _questionBank.GetVersionAsync(publishedId, ct)
                ?? throw new NotFoundException("نسخة بنك الأسئلة المنشورة غير موجودة.");

            var draft = Clone(source, await _questionBank.GetMaxVersionNoAsync(ct) + 1);
            _questionBank.AddVersion(draft);
            _log.Add("QUESTION_BANK_DRAFT", $"إنشاء مسودة بنك الأسئلة v{draft.VersionNo} من v{source.VersionNo}");
            await _unitOfWork.SaveChangesAsync(ct);
            return draft.Id;
        }

        public async Task DiscardDraftAsync(long versionId, CancellationToken ct = default)
        {
            var draft = await LoadDraftAsync(versionId, ct);
            foreach (var question in AllQuestions(draft)) ClearCondition(question);
            await _unitOfWork.SaveChangesAsync(ct); // self references are Restrict: break them before deleting

            _questionBank.RemoveVersion(draft);
            _log.Add("QUESTION_BANK_DISCARD", $"حذف مسودة بنك الأسئلة v{draft.VersionNo}");
            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task<PublishResult> PublishAsync(long versionId, string? notes, CancellationToken ct = default)
        {
            var adminId = _currentUser.RequireAdmin();
            var draft = await LoadDraftAsync(versionId, ct);
            var active = AllQuestions(draft).Where(q => q.IsActive).ToList();

            var errors = new List<string>();
            if (draft.Categories.Count == 0) errors.Add("لا توجد فئات في المسودة.");
            if (active.Count == 0) errors.Add("لا توجد أسئلة ظاهرة في المسودة.");
            var total = draft.Categories.Sum(c => c.BaseWeight);
            if (Math.Abs(total - 100m) > WeightSumTolerance)
                errors.Add($"مجموع أوزان الفئات {total:0.##}% ويجب أن يساوي 100%.");
            foreach (var q in active.Where(q => NeedsOptions(q.AnswerType) && q.Options.Count < MinOptions(q.AnswerType)))
                errors.Add($"السؤال {q.Code} يحتاج اختيارات قبل النشر.");
            if (errors.Count > 0) throw new BusinessRuleException(string.Join(" ", errors));

            var warnings = new List<string>();
            var activeCodes = active.Select(q => q.Code).ToHashSet(StringComparer.Ordinal);
            foreach (var q in active.Where(q => q.ParentQuestion is { IsActive: false }))
                warnings.Add($"السؤال {q.Code} متفرع من سؤال موقوف ({q.ParentQuestion!.Code}) ولن يظهر.");
            foreach (var group in (await _questionBank.GetRuleQuestionCodesAsync(ct))
                         .Where(r => !activeCodes.Contains(r.QuestionCode))
                         .GroupBy(r => r.QuestionCode))
                warnings.Add($"السؤال {group.Key} غير موجود أو موقوف، وتعتمد عليه: {string.Join("، ", group.Select(r => r.RuleCode).Distinct())}.");

            if (await _questionBank.GetPublishedVersionIdAsync(ct) is long publishedId)
            {
                var published = await _questionBank.GetVersionForUpdateAsync(publishedId, ct);
                if (published is not null) published.Status = QuestionBankVersionStatus.Archived;
            }
            draft.Status = QuestionBankVersionStatus.Published;
            draft.PublishedAt = _clock.GetUtcNow();
            draft.PublishedById = adminId;
            var cleanNotes = Text.Clean(notes, 1000);
            if (cleanNotes.Length > 0) draft.Notes = cleanNotes;

            _log.Add("QUESTION_BANK_UPDATE", $"نشر بنك الأسئلة v{draft.VersionNo}؛ الأسئلة الظاهرة: {active.Count}");
            await _unitOfWork.SaveChangesAsync(ct);
            return new PublishResult(draft.Id, draft.VersionNo, warnings);
        }

        // ------------------------------------------------------------------ categories

        public async Task SaveCategoryAsync(long versionId, long? categoryId, SaveCategoryRequest request, CancellationToken ct = default)
        {
            var draft = await LoadDraftAsync(versionId, ct);
            var title = Text.Clean(request.Title, 150);
            if (title.Length < 2) throw new BusinessRuleException("اسم الفئة يجب أن يكون حرفين على الأقل.");
            var status = Text.Clean(request.Status, 60);
            CheckRange(request.BaseWeight, 0, 100, "وزن الفئة يجب أن يكون بين 0 و100.");

            Category category;
            if (categoryId is null)
            {
                var code = Text.Clean(request.Code, 8);
                if (code.Length == 0) throw new BusinessRuleException("اكتبي كود الفئة.");
                if (draft.Categories.Any(c => c.Code == code)) throw new BusinessRuleException("كود الفئة مستخدم بالفعل.");
                category = new Category
                {
                    Code = code,
                    SortOrder = draft.Categories.Count == 0 ? 1 : draft.Categories.Max(c => c.SortOrder) + 1
                };
                draft.Categories.Add(category);
            }
            else
            {
                category = FindCategory(draft, categoryId.Value);
            }

            category.Title = title;
            category.Status = status.Length > 0 ? status : "معتمد";
            category.BaseWeight = request.BaseWeight;
            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task DeleteCategoryAsync(long versionId, long categoryId, CancellationToken ct = default)
        {
            var draft = await LoadDraftAsync(versionId, ct);
            var category = FindCategory(draft, categoryId);
            if (draft.Categories.Count == 1) throw new BusinessRuleException("لا يمكن حذف آخر فئة في بنك الأسئلة.");

            var removed = category.Questions.ToHashSet();
            foreach (var question in AllQuestions(draft).Where(q => q.ParentQuestion is not null && (removed.Contains(q.ParentQuestion) || removed.Contains(q))))
                ClearCondition(question);
            await _unitOfWork.SaveChangesAsync(ct);

            _questionBank.RemoveCategory(category);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task MoveCategoryAsync(long versionId, long categoryId, int direction, CancellationToken ct = default)
        {
            var draft = await LoadDraftAsync(versionId, ct);
            var ordered = draft.Categories.OrderBy(c => c.SortOrder).ToList();
            Swap(ordered, ordered.IndexOf(FindCategory(draft, categoryId)), Math.Sign(direction), c => c.SortOrder, (c, v) => c.SortOrder = v);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        // ------------------------------------------------------------------ questions

        public async Task SaveQuestionAsync(long versionId, long? questionId, SaveQuestionRequest request, CancellationToken ct = default)
        {
            var draft = await LoadDraftAsync(versionId, ct);
            var all = AllQuestions(draft).ToList();
            var category = draft.Categories.FirstOrDefault(c => c.Code == request.CategoryCode)
                ?? throw new BusinessRuleException("الفئة المحددة غير موجودة.");

            var code = Text.Clean(request.Code, 16).ToUpperInvariant();
            Question question;
            if (questionId is null)
            {
                if (!QuestionCodePattern.IsMatch(code))
                    throw new BusinessRuleException("كود السؤال يقبل الحروف الإنجليزية الكبيرة والأرقام فقط (حتى 16 خانة)، مثل C14.");
                if (all.Any(q => q.Code == code)) throw new BusinessRuleException($"كود السؤال {code} مستخدم بالفعل.");
                question = new Question { Code = code };
            }
            else
            {
                question = all.FirstOrDefault(q => q.Id == questionId.Value)
                    ?? throw new NotFoundException("السؤال المطلوب غير موجود في هذه النسخة.");
                if (code.Length > 0 && code != question.Code)
                    throw new BusinessRuleException("لا يمكن تغيير كود سؤال موجود؛ أضيفي سؤالًا جديدًا بدلًا منه.");
            }

            var text = Text.Clean(request.Text, 1000);
            if (text.Length < 2) throw new BusinessRuleException("اكتبي صياغة السؤال.");

            var options = ValidateOptions(request.AnswerType, request.Options);
            var isNumeric = request.AnswerType == AnswerType.Number || request.Kind == QuestionKind.Numeric;
            var bands = ValidateBands(isNumeric && options.Count == 0, request.Bands);
            var weights = ValidateWeights(request.Weights);

            // Branching
            Question? parent = null;
            ConditionOperator? op = null;
            string? conditionValue = null;
            var parentCode = Text.Clean(request.ParentCode, 16).ToUpperInvariant();
            if (parentCode.Length > 0)
            {
                parent = all.FirstOrDefault(q => q.Code == parentCode)
                    ?? throw new BusinessRuleException($"السؤال الأب {parentCode} غير موجود.");
                if (parent == question) throw new BusinessRuleException("لا يمكن أن يتفرع السؤال من نفسه.");
                for (var p = parent.ParentQuestion; p is not null; p = p.ParentQuestion)
                    if (p == question) throw new BusinessRuleException("هذا الشرط يصنع حلقة: السؤال الأب متفرع بالفعل من هذا السؤال.");

                op = request.ConditionOperator ?? ConditionOperator.Equals;
                conditionValue = Text.Clean(request.ConditionValue, 250);
                if (conditionValue.Length == 0) throw new BusinessRuleException("اكتبي قيمة شرط الظهور.");
                if (parent.Options.Count > 0 && parent.Options.All(o => o.Text != conditionValue))
                    throw new BusinessRuleException($"قيمة الشرط «{conditionValue}» ليست من اختيارات السؤال {parent.Code}.");
            }

            // Children that branch on a value this edit removes would never show again.
            if (questionId is not null && options.Count > 0)
            {
                var values = options.Select(o => o.Text).ToHashSet(StringComparer.Ordinal);
                var broken = all.Where(q => q.ParentQuestion == question && q.ConditionValue is not null && !values.Contains(q.ConditionValue)).ToList();
                if (broken.Count > 0)
                    throw new BusinessRuleException("لا يمكن حذف اختيار تعتمد عليه أسئلة متفرعة: "
                        + string.Join("، ", broken.Select(q => $"{q.Code} («{q.ConditionValue}»)")) + ".");
            }

            if (question.Category != category)
            {
                question.Category?.Questions.Remove(question);
                question.SortOrder = category.Questions.Count == 0 ? 1 : category.Questions.Max(q => q.SortOrder) + 1;
                category.Questions.Add(question);
            }

            question.Text = text;
            question.AnswerType = request.AnswerType;
            question.Kind = request.Kind;
            question.Direction = request.Direction;
            question.IsCritical = request.IsCritical;
            question.AllowCustomAnswer = request.AnswerType == AnswerType.Multi && request.AllowCustomAnswer;
            question.IsActive = request.IsActive;
            question.WeightConfidence = weights[0];
            question.WeightWorkflow = weights[1];
            question.WeightPricing = weights[2];
            question.ParentQuestion = parent;
            question.ParentQuestionId = parent?.Id;
            question.ConditionOperator = op;
            question.ConditionValue = conditionValue;

            question.Options.Clear();
            for (var i = 0; i < options.Count; i++)
                question.Options.Add(new QuestionOption { Text = options[i].Text, Score = options[i].Score, SortOrder = i + 1 });
            question.ScoreBands.Clear();
            for (var i = 0; i < bands.Count; i++)
                question.ScoreBands.Add(new QuestionScoreBand { MinValue = bands[i].MinValue, MaxValue = bands[i].MaxValue, Score = bands[i].Score, SortOrder = i + 1 });

            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task DeleteQuestionAsync(long versionId, long questionId, CancellationToken ct = default)
        {
            var draft = await LoadDraftAsync(versionId, ct);
            var all = AllQuestions(draft).ToList();
            var question = all.FirstOrDefault(q => q.Id == questionId)
                ?? throw new NotFoundException("السؤال المطلوب غير موجود في هذه النسخة.");

            foreach (var child in all.Where(q => q.ParentQuestion == question)) ClearCondition(child);
            ClearCondition(question);
            await _unitOfWork.SaveChangesAsync(ct);

            _questionBank.RemoveQuestion(question);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task MoveQuestionAsync(long versionId, long questionId, int direction, CancellationToken ct = default)
        {
            var draft = await LoadDraftAsync(versionId, ct);
            var question = AllQuestions(draft).FirstOrDefault(q => q.Id == questionId)
                ?? throw new NotFoundException("السؤال المطلوب غير موجود في هذه النسخة.");
            var ordered = question.Category.Questions.OrderBy(q => q.SortOrder).ToList();
            Swap(ordered, ordered.IndexOf(question), Math.Sign(direction), q => q.SortOrder, (q, v) => q.SortOrder = v);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        // ------------------------------------------------------------------ weights

        public async Task SaveCategoryWeightsAsync(long versionId, long categoryId, SaveCategoryWeightsRequest request, CancellationToken ct = default)
        {
            var draft = await LoadDraftAsync(versionId, ct);
            var category = FindCategory(draft, categoryId);
            CheckRange(request.BaseWeight, 0, 100, "وزن الفئة يجب أن يكون بين 0 و100.");

            var activityIds = (await _questionBank.GetActivitiesAsync(ct)).Select(a => a.Id).ToHashSet();
            foreach (var (activityId, factor) in request.Factors)
            {
                if (!activityIds.Contains(activityId)) throw new BusinessRuleException("نشاط غير معروف في معاملات النشاط.");
                CheckRange(factor, 0, 3, "معامل النشاط يجب أن يكون بين 0 و3.");
            }

            foreach (var (code, input) in request.Questions)
            {
                var question = category.Questions.FirstOrDefault(q => q.Code == code)
                    ?? throw new BusinessRuleException($"السؤال {code} ليس ضمن هذه الفئة.");
                var weights = ValidateWeights(new[] { input.Confidence, input.Workflow, input.Pricing });
                question.WeightConfidence = weights[0];
                question.WeightWorkflow = weights[1];
                question.WeightPricing = weights[2];
                question.IsCritical = input.IsCritical;
            }

            category.BaseWeight = request.BaseWeight;
            foreach (var (activityId, factor) in request.Factors)
            {
                var existing = category.ActivityFactors.FirstOrDefault(f => f.ActivityId == activityId);
                if (existing is null) category.ActivityFactors.Add(new ActivityFactor { ActivityId = activityId, Factor = factor });
                else existing.Factor = factor;
            }

            await _unitOfWork.SaveChangesAsync(ct);
        }

        // ------------------------------------------------------------------ helpers

        private async Task<QuestionBankVersion> LoadDraftAsync(long versionId, CancellationToken ct)
        {
            _currentUser.RequireAdmin();
            var version = await _questionBank.GetVersionForUpdateAsync(versionId, ct)
                ?? throw new NotFoundException("نسخة بنك الأسئلة المطلوبة غير موجودة.");
            if (version.Status != QuestionBankVersionStatus.Draft)
                throw new BusinessRuleException("هذه النسخة منشورة ولا يمكن تعديلها. أنشئي مسودة جديدة أولًا.");
            return version;
        }

        private static IEnumerable<Question> AllQuestions(QuestionBankVersion version) =>
            version.Categories.SelectMany(c => c.Questions);

        private static Category FindCategory(QuestionBankVersion version, long categoryId) =>
            version.Categories.FirstOrDefault(c => c.Id == categoryId)
            ?? throw new NotFoundException("الفئة المطلوبة غير موجودة في هذه النسخة.");

        private static void ClearCondition(Question question)
        {
            question.ParentQuestion = null;
            question.ParentQuestionId = null;
            question.ConditionOperator = null;
            question.ConditionValue = null;
        }

        private static bool NeedsOptions(AnswerType type) => type is AnswerType.YesNo or AnswerType.Choice or AnswerType.Multi;

        private static int MinOptions(AnswerType type) => type == AnswerType.Multi ? 1 : 2;

        private static List<OptionInput> ValidateOptions(AnswerType type, IReadOnlyList<OptionInput>? input)
        {
            var options = (input ?? Array.Empty<OptionInput>())
                .Select(o => new OptionInput(Text.Clean(o.Text, 250), o.Score))
                .Where(o => o.Text.Length > 0)
                .ToList();

            if (!NeedsOptions(type))
            {
                if (options.Count > 0) throw new BusinessRuleException("الأسئلة النصية والرقمية والتاريخ لا تحتوي على اختيارات.");
                return options;
            }
            if (options.Count < MinOptions(type)) throw new BusinessRuleException(type == AnswerType.Multi
                ? "سؤال الاختيار المتعدد يحتاج اختيارًا واحدًا على الأقل."
                : "سؤال الاختيار يحتاج اختيارين على الأقل.");
            if (options.Count > 100) throw new BusinessRuleException("عدد الاختيارات أكبر من المسموح (100).");
            var duplicate = options.GroupBy(o => o.Text).FirstOrDefault(g => g.Count() > 1);
            if (duplicate is not null) throw new BusinessRuleException($"الاختيار «{duplicate.Key}» مكرر.");
            foreach (var o in options.Where(o => o.Score is not null))
                CheckRange(o.Score!.Value, 0, 1, $"درجة الاختيار «{o.Text}» يجب أن تكون بين 0 و1، أو فارغة لـ«غير منطبق».");
            return options;
        }

        private static List<BandInput> ValidateBands(bool allowed, IReadOnlyList<BandInput>? input)
        {
            var bands = (input ?? Array.Empty<BandInput>()).ToList();
            if (bands.Count == 0) return bands;
            if (!allowed) throw new BusinessRuleException("شرائح الدرجات متاحة للأسئلة الرقمية بدون اختيارات فقط.");
            if (bands.Count > 20) throw new BusinessRuleException("عدد الشرائح أكبر من المسموح (20).");

            foreach (var b in bands)
            {
                CheckRange(b.Score, 0, 1, "درجة الشريحة يجب أن تكون بين 0 و1.");
                if (b.MinValue is not null && b.MaxValue is not null && b.MinValue >= b.MaxValue)
                    throw new BusinessRuleException($"بداية الشريحة ({b.MinValue}) يجب أن تكون أقل من نهايتها ({b.MaxValue}).");
            }

            var ordered = bands.OrderBy(b => b.MinValue ?? decimal.MinValue).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                var previousEnd = ordered[i - 1].MaxValue ?? decimal.MaxValue;
                var start = ordered[i].MinValue ?? decimal.MinValue;
                if (start < previousEnd) throw new BusinessRuleException("الشرائح متداخلة؛ كل قيمة يجب أن تقع في شريحة واحدة فقط.");
            }
            return ordered;
        }

        private static decimal[] ValidateWeights(decimal[]? weights)
        {
            if (weights is null || weights.Length != 3) throw new BusinessRuleException("أوزان السؤال غير مكتملة.");
            foreach (var w in weights) CheckRange(w, 0, 5, "أوزان السؤال يجب أن تكون بين 0 و5.");
            return weights;
        }

        private static void CheckRange(decimal value, decimal min, decimal max, string message)
        {
            if (value < min || value > max) throw new BusinessRuleException(message);
        }

        private static void Swap<T>(List<T> ordered, int index, int direction, Func<T, int> get, Action<T, int> set)
        {
            var target = index + direction;
            if (index < 0 || direction == 0 || target < 0 || target >= ordered.Count) return;
            // Renumber first so equal or missing sort orders cannot block the swap.
            for (var i = 0; i < ordered.Count; i++) set(ordered[i], i + 1);
            var a = get(ordered[index]);
            set(ordered[index], get(ordered[target]));
            set(ordered[target], a);
        }

        /// <summary>Deep copy of a version (read without tracking) into a new draft.</summary>
        private static QuestionBankVersion Clone(QuestionBankVersion source, int versionNo)
        {
            var draft = new QuestionBankVersion
            {
                VersionNo = versionNo,
                Status = QuestionBankVersionStatus.Draft,
                Notes = $"مسودة من النسخة v{source.VersionNo}"
            };
            var copies = new Dictionary<long, Question>();
            var parents = new List<(Question Copy, long ParentId)>();

            foreach (var c in source.Categories.OrderBy(c => c.SortOrder))
            {
                var category = new Category
                {
                    Code = c.Code, Title = c.Title, Status = c.Status, SortOrder = c.SortOrder, BaseWeight = c.BaseWeight,
                    ActivityFactors = c.ActivityFactors.Select(f => new ActivityFactor { ActivityId = f.ActivityId, Factor = f.Factor }).ToList()
                };
                foreach (var q in c.Questions.OrderBy(q => q.SortOrder))
                {
                    var copy = new Question
                    {
                        Code = q.Code, Text = q.Text, SortOrder = q.SortOrder, AnswerType = q.AnswerType, Kind = q.Kind,
                        Direction = q.Direction, IsCritical = q.IsCritical, AllowCustomAnswer = q.AllowCustomAnswer, IsActive = q.IsActive,
                        WeightConfidence = q.WeightConfidence, WeightWorkflow = q.WeightWorkflow, WeightPricing = q.WeightPricing,
                        ConditionOperator = q.ConditionOperator, ConditionValue = q.ConditionValue,
                        Options = q.Options.Select(o => new QuestionOption { Text = o.Text, Score = o.Score, SortOrder = o.SortOrder }).ToList(),
                        ScoreBands = q.ScoreBands.Select(b => new QuestionScoreBand { MinValue = b.MinValue, MaxValue = b.MaxValue, Score = b.Score, SortOrder = b.SortOrder }).ToList()
                    };
                    category.Questions.Add(copy);
                    copies[q.Id] = copy;
                    if (q.ParentQuestionId is long parentId) parents.Add((copy, parentId));
                }
                draft.Categories.Add(category);
            }

            foreach (var (copy, parentId) in parents)
                if (copies.TryGetValue(parentId, out var parent)) copy.ParentQuestion = parent;
            return draft;
        }
    }
}
