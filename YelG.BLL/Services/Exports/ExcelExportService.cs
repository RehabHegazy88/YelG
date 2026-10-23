using YelG.BLL.Common;
using YelG.BLL.IRepository;
using YelG.BLL.Scoring;
using YelG.BLL.Services.Audit;
using YelG.BLL.ViewModel;
using YelG.DAL.Abstractions;
using YelG.Model.Enums;
using YelG.Model.Evaluations;
using YelG.Model.QuestionBank;
using YelG.Model.Scope;

namespace YelG.BLL.Services.Exports
{
    public sealed class ExcelExportService : IExcelExportService
    {
        /// <summary>Most log rows in one file; narrow the dates for more.</summary>
        public const int MaxLogRows = 20000;

        private const string Percent = "0.0";
        private const string Hours = "0.##";
        private const string Money = "#,##0";
        private const string DateTimeFormat = "yyyy-mm-dd hh:mm";
        private const string DateFormat = "yyyy-mm-dd";

        private readonly IEvaluationRepository _evaluations;
        private readonly IQuestionBankRepository _questionBank;
        private readonly IScopeRepository _scope;
        private readonly IActivityLogRepository _log;
        private readonly IUserRepository _users;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly TimeProvider _clock;

        public ExcelExportService(
            IEvaluationRepository evaluations,
            IQuestionBankRepository questionBank,
            IScopeRepository scope,
            IActivityLogRepository log,
            IUserRepository users,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            TimeProvider clock)
        {
            _evaluations = evaluations;
            _questionBank = questionBank;
            _scope = scope;
            _log = log;
            _users = users;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _clock = clock;
        }

        public async Task<ExportFile> ExportResultsAsync(CancellationToken ct = default)
        {
            var evaluations = await VisibleEvaluationsAsync(ct);
            var models = new ModelCache(_questionBank);

            var summary = new List<object?[]>();
            var categories = new List<object?[]>();
            var answers = new List<object?[]>();

            foreach (var e in evaluations)
            {
                var (version, model) = await models.GetAsync(e.QuestionBankVersionId, ct);
                var set = e.Answers.ToAnswerSet();
                var score = ScoringEngine.Score(model, e.Company.ActivityId, set);
                var breaches = score.Breaches.Select(q => q.Code).ToHashSet();
                var company = e.Company.Name;
                var reviewer = e.Reviewer.DisplayName;

                summary.Add(new object?[]
                {
                    company, e.Company.Activity.ArabicName, reviewer, Labels.Of(e.Status), score.Answered, score.VisibleCount,
                    Math.Round(score.Confidence, 1), Labels.Of(score.Grade), score.Breaches.Count,
                    string.Join("، ", score.Breaches.Select(q => q.Code)),
                    e.ProfessionalJudgment is { } judgment ? Labels.Of(judgment) : "لم يُحدَّد",
                    e.ProfessionalJudgment is null ? "—" : e.ProfessionalJudgment == score.Grade ? "مطابق" : "مختلف",
                    score.Unscored.Count, "v" + version.VersionNo,
                    Cairo(e.SubmittedAt), Cairo(e.ReviewedAt), e.Note
                });

                foreach (var c in score.Categories)
                    categories.Add(new object?[]
                    {
                        company, reviewer, c.Code, c.Title, Round(c.Confidence), Round(c.Workflow), Round(c.Pricing),
                        Math.Round(c.RelativeWeight, 1), c.Contribution is { } contribution ? Math.Round(contribution, 2) : null
                    });

                foreach (var category in model.Categories)
                foreach (var q in category.Questions)
                {
                    var values = set.ValuesOf(q.Code);
                    if (values.Count == 0 || !ScoringEngine.IsVisible(model, q, set)) continue;
                    var value = ScoringEngine.ValueOf(q, set);
                    answers.Add(new object?[]
                    {
                        company, reviewer, q.Code, category.Title, q.Text, Labels.Of(q.Kind), string.Join("، ", values),
                        value is { } v ? Math.Round(v, 2) : null, q.IsCritical, breaches.Contains(q.Code)
                    });
                }
            }

            using var book = new WorkbookBuilder()
                .AddSheet("ملخص التقييمات", new Column[]
                {
                    new("الشركة"), new("النشاط"), new("المقيم"), new("الحالة"), new("الأسئلة المجابة"), new("الأسئلة الظاهرة"),
                    new("درجة الثقة %", Percent), new("التصنيف"), new("مخالفات حرجة"), new("أسئلة المخالفات"),
                    new("الحكم المهني"), new("مطابقة الحكم"), new("أسئلة رقمية بلا درجة"), new("نسخة بنك الأسئلة"),
                    new("تاريخ الإرسال", DateTimeFormat), new("تاريخ المراجعة", DateTimeFormat), new("ملاحظة", Wrap: true)
                }, summary)
                .AddSheet("نتائج الفئات", new Column[]
                {
                    new("الشركة"), new("المقيم"), new("كود الفئة"), new("الفئة"), new("الثقة %", Percent),
                    new("سير العمل %", Percent), new("التسعير %", Percent), new("الوزن النسبي %", Percent),
                    new("المساهمة في الثقة", "0.00")
                }, categories)
                .AddSheet("الإجابات", new Column[]
                {
                    new("الشركة"), new("المقيم"), new("الكود"), new("الفئة"), new("السؤال", Wrap: true), new("نوع السؤال"),
                    new("الإجابة", Wrap: true), new("الدرجة (0 – 1)", "0.00"), new("حرج"), new("مخالفة حرجة")
                }, answers);

            return await FinishAsync(book, "YELG-Evaluation-Results", $"تصدير نتائج {evaluations.Count} تقييم", ct);
        }

        public async Task<ExportFile> ExportScopeAsync(long? evaluationId, CancellationToken ct = default)
        {
            List<Evaluation> evaluations;
            if (evaluationId is long id)
            {
                var userId = _currentUser.RequireUserId();
                var evaluation = await _evaluations.GetWithAnswersAsync(id, ct)
                    ?? throw new NotFoundException("التقييم المطلوب غير موجود.");
                if (evaluation.ReviewerId != userId && !_currentUser.IsAdmin) throw new ForbiddenException();
                evaluations = new() { evaluation };
            }
            else
            {
                evaluations = await VisibleEvaluationsAsync(ct);
            }

            var models = new ModelCache(_questionBank);
            var templates = await _scope.GetTemplatesAsync(ct);
            var rules = await _scope.GetRulesAsync(ct);
            var pricing = await _scope.GetPricingSettingsAsync(ct);

            var summary = new List<object?[]>();
            var tasks = new List<object?[]>();
            var effects = new List<object?[]>();

            foreach (var e in evaluations)
            {
                var (_, model) = await models.GetAsync(e.QuestionBankVersionId, ct);
                var states = (await _scope.GetScopeItemsAsync(e.Id, ct))
                    .ToDictionary(s => (s.TaskTemplateId, s.IsOptionalService), ScopeItemState.From);
                var scope = ScopeEngine.Generate(model, e.Answers.ToAnswerSet(), templates, rules, states);
                var company = e.Company.Name;
                var reviewer = e.Reviewer.DisplayName;
                var status = Labels.Of(e.Status);

                var price = ScopeEngine.FirstYearPrice(scope.Tasks, pricing);
                var unestimated = scope.Tasks.Count(t => t.Hours == 0);
                summary.Add(new object?[]
                {
                    company, e.Company.Activity.ArabicName, reviewer, status, scope.Tasks.Count,
                    scope.Tasks.Where(t => t.Template.Frequency == TaskFrequency.OneTime).Sum(t => t.Hours),
                    scope.Tasks.Where(t => t.Template.Frequency != TaskFrequency.OneTime).Sum(t => t.AnnualHours),
                    scope.Tasks.Sum(t => t.AnnualHours), unestimated,
                    price is { } p ? Math.Round(p) : null,
                    price is not null ? null
                        : scope.Tasks.Count == 0 ? "لا توجد مهام"
                        : unestimated > 0 ? $"{unestimated} مهمة تحتاج تقدير ساعات"
                        : "معدلات الأدوار لم تُحدَّد بعد"
                });

                foreach (var task in scope.Tasks)
                {
                    var t = task.Template;
                    tasks.Add(new object?[]
                    {
                        company, reviewer, status, t.Code, t.SourceQuestionCode, t.Title, t.ServiceLine, t.Purpose,
                        Labels.Of(task.Priority), Labels.Of(t.Frequency), string.Join(" · ", task.Sources), t.Evidence,
                        task.RoleHours.Analyst, task.RoleHours.TeamLead, task.RoleHours.Manager, task.RoleHours.Partner,
                        task.Hours, task.VolumeLabel, task.AnnualMultiplier, task.AnnualHours,
                        t.BasisStatus == BasisStatus.Approved ? Labels.Of(t.BasisStatus) : t.BasisNote ?? Labels.Of(t.BasisStatus),
                        Labels.Of(task.State.Owner), task.State.DueDate, Labels.Of(task.State.Status), task.State.EvidenceNote
                    });
                }

                foreach (var rec in scope.Recommendations)
                    effects.Add(new object?[] { company, reviewer, "توصية", rec.Text, rec.Source, Labels.Of(rec.Priority), "لا تضيف ساعات" });
                foreach (var service in scope.OptionalServices)
                    effects.Add(new object?[]
                    {
                        company, reviewer, "خدمة اختيارية", service.Template.Title, service.Source, Labels.Of(service.Rule.Priority),
                        service.State.OptionalIncluded ? "مضافة للنطاق" : "غير محتسبة"
                    });
            }

            using var book = new WorkbookBuilder()
                .AddSheet("ملخص النطاق", new Column[]
                {
                    new("الشركة"), new("النشاط"), new("المقيم"), new("حالة التقييم"), new("عدد المهام"),
                    new("ساعات لمرة واحدة", Hours), new("ساعات متكررة سنويًا", Hours), new("ساعات السنة الأولى", Hours),
                    new("مهام بلا تقدير ساعات"), new("تكلفة السنة الأولى (استرشادية)", Money), new("سبب عدم توفر التكلفة")
                }, summary)
                .AddSheet("نطاق العمل", new Column[]
                {
                    new("الشركة"), new("المقيم"), new("حالة التقييم"), new("كود المهمة"), new("السؤال المصدر"),
                    new("المهمة", Wrap: true), new("خط الخدمة"), new("الغرض"), new("الأولوية"), new("الدورية"),
                    new("سبب الظهور", Wrap: true), new("منطق التنفيذ", Wrap: true),
                    new("Analyst", Hours), new("Team Leader", Hours), new("Manager", Hours), new("Partner", Hours),
                    new("ساعات لكل تنفيذ", Hours), new("معامل الحجم"), new("مرات التنفيذ سنويًا"), new("ساعات سنوية", Hours),
                    new("أساس التقدير", Wrap: true), new("جهة التنفيذ"), new("الموعد المستهدف", DateFormat),
                    new("حالة التنفيذ"), new("دليل الإنجاز", Wrap: true)
                }, tasks)
                .AddSheet("توصيات وخدمات اختيارية", new Column[]
                {
                    new("الشركة"), new("المقيم"), new("النوع"), new("التوصية / الخدمة", Wrap: true), new("المصدر", Wrap: true),
                    new("الأولوية"), new("الحالة")
                }, effects)
                .AddSheet("أساس التسعير", new Column[] { new("البند"), new("القيمة", "#,##0.##"), new("ملاحظة", Wrap: true) },
                    PricingRows(pricing));

            var name = evaluations.Count == 1 ? "YELG-Scope-of-Work-" + evaluations[0].Id : "YELG-Scope-of-Work";
            var details = evaluations.Count == 1
                ? $"تصدير نطاق العمل: {evaluations[0].Company.Name}"
                : $"تصدير نطاق العمل لعدد {evaluations.Count} تقييم";
            return await FinishAsync(book, name, details, ct);
        }

        public async Task<ExportFile> ExportQuestionBankAsync(long versionId, CancellationToken ct = default)
        {
            _currentUser.RequireAdmin();
            var version = await _questionBank.GetVersionAsync(versionId, ct)
                ?? throw new NotFoundException("نسخة بنك الأسئلة المطلوبة غير موجودة.");
            var activities = await _questionBank.GetActivitiesAsync(ct);

            var orderedCategories = version.Categories.OrderBy(c => c.SortOrder).ToList();
            var questions = orderedCategories.SelectMany(c => c.Questions.OrderBy(q => q.SortOrder)).ToList();
            var codeById = questions.ToDictionary(q => q.Id, q => q.Code);

            var categoryColumns = new List<Column> { new("الكود"), new("الفئة"), new("الحالة"), new("الوزن الأساسي %", "0.####") };
            categoryColumns.AddRange(activities.Select(a => new Column("معامل " + a.ArabicName, "0.##")));
            var categoryRows = orderedCategories.Select(c =>
            {
                var factors = c.ActivityFactors.ToDictionary(f => f.ActivityId, f => f.Factor);
                return new object?[] { c.Code, c.Title, c.Status, c.BaseWeight }
                    .Concat(activities.Select(a => (object?)(factors.TryGetValue(a.Id, out var f) ? f : 1m)))
                    .ToArray();
            });

            using var book = new WorkbookBuilder()
                .AddSheet("الفئات والأوزان", categoryColumns, categoryRows)
                .AddSheet("الأسئلة", new Column[]
                {
                    new("الكود"), new("كود الفئة"), new("الفئة"), new("السؤال", Wrap: true), new("نوع الإجابة"), new("النوع"),
                    new("الاتجاه"), new("حرج"), new("وزن الثقة", "0.##"), new("وزن سير العمل", "0.##"), new("وزن التسعير", "0.##"),
                    new("يظهر بعد السؤال"), new("شرط الظهور", Wrap: true), new("يقبل إجابة أخرى"), new("نشط"), new("عدد الاختيارات")
                }, questions.Select(q => new object?[]
                {
                    q.Code, q.Category.Code, q.Category.Title, q.Text, Labels.Of(q.AnswerType), Labels.Of(q.Kind),
                    Labels.Of(q.Direction), q.IsCritical, q.WeightConfidence, q.WeightWorkflow, q.WeightPricing,
                    q.ParentQuestionId is long parent && codeById.TryGetValue(parent, out var parentCode) ? parentCode : null,
                    q.ConditionOperator is { } op ? $"{Labels.Of(op)} {q.ConditionValue}" : null,
                    q.AllowCustomAnswer, q.IsActive, q.Options.Count
                }))
                .AddSheet("الاختيارات", new Column[] { new("كود السؤال"), new("الترتيب"), new("الاختيار", Wrap: true), new("الدرجة", "0.##") },
                    questions.SelectMany(q => q.Options.OrderBy(o => o.SortOrder)
                        .Select(o => new object?[] { q.Code, o.SortOrder, o.Text, o.Score })))
                .AddSheet("نطاقات الأرقام", new Column[] { new("كود السؤال"), new("من", "#,##0.##"), new("إلى", "#,##0.##"), new("الدرجة", "0.##") },
                    questions.SelectMany(q => q.ScoreBands.OrderBy(b => b.SortOrder)
                        .Select(b => new object?[] { q.Code, b.MinValue, b.MaxValue, b.Score })));

            return await FinishAsync(book, $"YELG-Question-Bank-v{version.VersionNo}",
                $"تصدير بنك الأسئلة v{version.VersionNo}", ct);
        }

        public async Task<ExportFile> ExportTaskLibraryAsync(CancellationToken ct = default)
        {
            _currentUser.RequireAdmin();
            var templates = await _scope.GetTemplatesAsync(ct);
            var rules = await _scope.GetRulesAsync(ct);
            var pricing = await _scope.GetPricingSettingsAsync(ct);
            var templateById = templates.ToDictionary(t => t.Id);

            using var book = new WorkbookBuilder()
                .AddSheet("المهام", new Column[]
                {
                    new("الكود"), new("المهمة", Wrap: true), new("السؤال المصدر"), new("خط الخدمة"), new("الغرض"),
                    new("منطق التنفيذ", Wrap: true), new("جهة التنفيذ الافتراضية"), new("الأولوية"), new("الدورية"),
                    new("Analyst", Hours), new("Team Leader", Hours), new("Manager", Hours), new("Partner", Hours),
                    new("إجمالي ساعات التنفيذ", Hours), new("سؤال الحجم"), new("وحدة الحجم", "#,##0.##"), new("أقصى مضاعف"),
                    new("ضمن البرنامج الأساسي"), new("أساس التقدير"), new("ملاحظة الأساس", Wrap: true), new("نشطة")
                }, templates.Select(t => new object?[]
                {
                    t.Code, t.Title, t.SourceQuestionCode, t.ServiceLine, t.Purpose, t.Evidence, Labels.Of(t.DefaultOwner),
                    Labels.Of(t.Priority), Labels.Of(t.Frequency),
                    t.RoleHours.Analyst, t.RoleHours.TeamLead, t.RoleHours.Manager, t.RoleHours.Partner, Total(t.RoleHours),
                    t.DriverQuestionCode, t.DriverQuestionCode is null ? null : t.DriverUnit,
                    t.DriverQuestionCode is null ? null : t.MaxMultiplier,
                    t.DefaultIncluded, Labels.Of(t.BasisStatus), t.BasisNote, t.IsActive
                }))
                .AddSheet("تأثير الإجابات", new Column[]
                {
                    new("الكود"), new("السؤال"), new("الشرط"), new("القيمة", Wrap: true), new("التأثير"), new("كود المهمة"),
                    new("المهمة", Wrap: true), new("Analyst +", Hours), new("Team Leader +", Hours), new("Manager +", Hours),
                    new("Partner +", Hours), new("التوصية", Wrap: true), new("ملاحظة الشرط", Wrap: true), new("الأولوية"), new("نشطة")
                }, rules.Select(r =>
                {
                    var target = r.TaskTemplateId is long id && templateById.TryGetValue(id, out var t) ? t : null;
                    return new object?[]
                    {
                        r.Code, r.QuestionCode, Labels.Of(r.Operator), r.Value?.Replace("|", " · "), Labels.Of(r.EffectType),
                        target?.Code, target?.Title, r.RoleDelta.Analyst, r.RoleDelta.TeamLead, r.RoleDelta.Manager,
                        r.RoleDelta.Partner, r.Recommendation, r.ConditionNote, Labels.Of(r.Priority), r.IsActive
                    };
                }))
                .AddSheet("المعدلات", new Column[] { new("البند"), new("القيمة", "#,##0.##"), new("ملاحظة", Wrap: true) },
                    PricingRows(pricing));

            return await FinishAsync(book, "YELG-Task-Library",
                $"تصدير مكتبة المهام: {templates.Count} مهمة و{rules.Count} قاعدة", ct);
        }

        public async Task<ExportFile> ExportActivityLogAsync(ActivityLogFilter filter, CancellationToken ct = default)
        {
            _currentUser.RequireAdmin();
            var users = (await _users.ListWithRolesAsync(ct)).Select(u => new UserChoice(u.User.Id, u.User.DisplayName)).ToList();
            var (rows, total) = await _log.SearchAsync(ActivityLogService.ToQuery(filter), 0, MaxLogRows, ct);

            using var book = new WorkbookBuilder()
                .AddSheet("سجل النشاط", new Column[]
                {
                    new("الوقت", DateTimeFormat), new("المستخدم"), new("المجموعة"), new("العملية"), new("الكود"),
                    new("التفاصيل", Wrap: true)
                }, ActivityLogService.ToRows(rows, users).Select(r => new object?[]
                {
                    Labels.ToCairo(r.OccurredAt), r.UserName, r.Group, r.ActionLabel, r.Action, r.Details
                }));

            var details = total > rows.Count
                ? $"تصدير سجل النشاط: أحدث {rows.Count} من {total} عملية"
                : $"تصدير سجل النشاط: {rows.Count} عملية";
            return await FinishAsync(book, "YELG-Activity-Log", details, ct);
        }

        private async Task<List<Evaluation>> VisibleEvaluationsAsync(CancellationToken ct)
        {
            var userId = _currentUser.RequireUserId();
            var list = await _evaluations.ListAsync(_currentUser.IsAdmin ? null : userId, ct);
            return list
                .OrderBy(e => e.Company.Name, StringComparer.CurrentCulture)
                .ThenBy(e => e.Reviewer.DisplayName, StringComparer.CurrentCulture)
                .ToList();
        }

        private static IEnumerable<object?[]> PricingRows(PricingSettings pricing)
        {
            var rates = pricing.HourlyRates;
            yield return new object?[] { "معدل ساعة Analyst", rates.Analyst, null };
            yield return new object?[] { "معدل ساعة Team Leader", rates.TeamLead, null };
            yield return new object?[] { "معدل ساعة Manager", rates.Manager, null };
            yield return new object?[] { "معدل ساعة Partner", rates.Partner, null };
            yield return new object?[] { "هامش التسعير %", pricing.MarginPct, null };
            yield return new object?[] { "حالة المنهجية", null, $"{Labels.Of(pricing.MethodologyStatus)} — {pricing.MethodologyNote}".TrimEnd(' ', '—') };
        }

        private async Task<ExportFile> FinishAsync(WorkbookBuilder book, string name, string details, CancellationToken ct)
        {
            var content = book.ToBytes();
            _log.Add("EXPORT_EXCEL", details);
            await _unitOfWork.SaveChangesAsync(ct);

            var stamp = Labels.ToCairo(_clock.GetUtcNow()).ToString("yyyyMMdd-HHmm");
            return new ExportFile($"{name}-{stamp}.xlsx", WorkbookBuilder.ContentType, content);
        }

        private static DateTime? Cairo(DateTimeOffset? value) => value is { } v ? Labels.ToCairo(v) : null;

        private static double? Round(double? value) => value is { } v ? Math.Round(v, 1) : null;

        private static decimal Total(RoleHours hours) => hours.Analyst + hours.TeamLead + hours.Manager + hours.Partner;

        /// <summary>Loads each question bank version once, however many evaluations use it.</summary>
        private sealed class ModelCache
        {
            private readonly IQuestionBankRepository _repository;
            private readonly Dictionary<long, (QuestionBankVersion, ScoringModel)> _cache = new();

            public ModelCache(IQuestionBankRepository repository) => _repository = repository;

            public async Task<(QuestionBankVersion Version, ScoringModel Model)> GetAsync(long versionId, CancellationToken ct)
            {
                if (_cache.TryGetValue(versionId, out var hit)) return hit;
                var version = await _repository.GetVersionAsync(versionId, ct)
                    ?? throw new InvalidOperationException("نسخة بنك الأسئلة المطلوبة غير موجودة.");
                return _cache[versionId] = (version, version.ToScoringModel());
            }
        }
    }
}
