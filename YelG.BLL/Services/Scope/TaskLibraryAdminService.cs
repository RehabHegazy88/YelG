using System.Globalization;
using System.Text.RegularExpressions;
using YelG.BLL.Common;
using YelG.BLL.IRepository;
using YelG.BLL.Scoring;
using YelG.BLL.ViewModel;
using YelG.DAL.Abstractions;
using YelG.Model.Enums;
using YelG.Model.Scope;

namespace YelG.BLL.Services.Scope
{
    public sealed class TaskLibraryAdminService : ITaskLibraryAdminService
    {
        private const decimal MaxRoleHours = 500m;
        private static readonly Regex CodePattern = new("^[A-Z0-9-]{1,20}$", RegexOptions.Compiled);

        private readonly IScopeRepository _scope;
        private readonly IQuestionBankRepository _questionBank;
        private readonly IActivityLogRepository _log;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly TimeProvider _clock;

        public TaskLibraryAdminService(
            IScopeRepository scope,
            IQuestionBankRepository questionBank,
            IActivityLogRepository log,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            TimeProvider clock)
        {
            _scope = scope;
            _questionBank = questionBank;
            _log = log;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _clock = clock;
        }

        public async Task<TaskLibrary> GetLibraryAsync(CancellationToken ct = default)
        {
            _currentUser.RequireAdmin();
            var templates = await _scope.GetTemplatesAsync(ct);
            var rules = await _scope.GetRulesAsync(ct);
            var used = await _scope.GetUsedTemplateIdsAsync(ct);
            var questions = await PublishedQuestionsAsync(ct);
            var questionByCode = questions.ToDictionary(q => q.Code);
            var templateById = templates.ToDictionary(t => t.Id);
            var ruleCount = rules.Where(r => r.TaskTemplateId is not null)
                .GroupBy(r => r.TaskTemplateId!.Value).ToDictionary(g => g.Key, g => g.Count());
            var pricing = await _scope.GetPricingSettingsAsync(ct);

            return new TaskLibrary(
                templates.Select(t => new TemplateEditor(
                    t.Id, t.Code, t.Title, t.ServiceLine, t.Evidence, t.Purpose, t.DefaultOwner, t.SourceQuestionCode,
                    ToInput(t.RoleHours), t.DriverQuestionCode, t.DriverUnit, t.MaxMultiplier, t.Frequency, t.Priority,
                    t.DefaultIncluded, t.IsActive, t.BasisStatus, t.BasisNote,
                    ruleCount.TryGetValue(t.Id, out var n) ? n : 0, used.Contains(t.Id))).ToList(),
                rules.Select(r =>
                {
                    var target = r.TaskTemplateId is long id && templateById.TryGetValue(id, out var t) ? t : null;
                    var question = questionByCode.TryGetValue(r.QuestionCode, out var q) ? q : null;
                    return new RuleEditor(r.Id, r.Code, r.QuestionCode, question?.Text, r.Operator, r.Value, r.EffectType,
                        r.TaskTemplateId, target?.Code, target?.Title, ToInput(r.RoleDelta), r.Recommendation, r.ConditionNote,
                        r.Priority, r.IsActive, question is null);
                }).ToList(),
                questions,
                new PricingEditor(ToInput(pricing.HourlyRates), pricing.MarginPct, pricing.MethodologyStatus, pricing.MethodologyNote));
        }

        // ------------------------------------------------------------------ templates

        public async Task<long> SaveTemplateAsync(long? templateId, SaveTemplateRequest request, CancellationToken ct = default)
        {
            _currentUser.RequireAdmin();
            var questions = (await PublishedQuestionsAsync(ct)).ToDictionary(q => q.Code);

            TaskTemplate template;
            if (templateId is null)
            {
                var code = Text.Clean(request.Code, 20).ToUpperInvariant();
                if (!CodePattern.IsMatch(code))
                    throw new BusinessRuleException("كود المهمة يقبل الحروف الإنجليزية الكبيرة والأرقام والشرطة فقط (حتى 20 خانة)، مثل XL-C15.");
                if (await _scope.TemplateCodeExistsAsync(code, ct)) throw new BusinessRuleException($"كود المهمة {code} مستخدم بالفعل.");
                template = new TaskTemplate { Code = code };
                _scope.AddTemplate(template);
            }
            else
            {
                template = await _scope.GetTemplateForUpdateAsync(templateId.Value, ct)
                    ?? throw new NotFoundException("المهمة المطلوبة غير موجودة.");
            }

            var title = Text.Clean(request.Title, 600);
            if (title.Length < 2) throw new BusinessRuleException("اكتبي صياغة المهمة.");

            var driver = Text.Clean(request.DriverQuestionCode, 16).ToUpperInvariant();
            if (driver.Length > 0)
            {
                if (!questions.TryGetValue(driver, out var question))
                    throw new BusinessRuleException($"سؤال محرك الحجم {driver} غير موجود في بنك الأسئلة المنشور.");
                if (!question.IsNumeric)
                    throw new BusinessRuleException($"محرك الحجم يجب أن يكون سؤالًا رقميًا، و{driver} ليس كذلك.");
                if (request.DriverUnit < 1) throw new BusinessRuleException("عدد الوحدات لكل شريحة يجب أن يكون 1 على الأقل.");
                if (request.MaxMultiplier is < 1 or > 20) throw new BusinessRuleException("أقصى مضاعف للحجم يجب أن يكون بين 1 و20.");
            }

            var source = Text.Clean(request.SourceQuestionCode, 16).ToUpperInvariant();

            template.Title = title;
            template.ServiceLine = NullIfEmpty(Text.Clean(request.ServiceLine, 120));
            template.Evidence = NullIfEmpty(Text.CleanMultiline(request.Evidence, 2000));
            template.Purpose = NullIfEmpty(Text.Clean(request.Purpose, 120));
            template.DefaultOwner = request.DefaultOwner;
            template.SourceQuestionCode = NullIfEmpty(source);
            template.RoleHours = ValidateHours(request.RoleHours, "ساعات");
            template.DriverQuestionCode = NullIfEmpty(driver);
            template.DriverUnit = driver.Length > 0 ? request.DriverUnit : 1;
            template.MaxMultiplier = driver.Length > 0 ? request.MaxMultiplier : 1;
            template.Frequency = request.Frequency;
            template.Priority = request.Priority;
            template.DefaultIncluded = request.DefaultIncluded;
            template.IsActive = request.IsActive;
            template.BasisStatus = request.BasisStatus;
            template.BasisNote = NullIfEmpty(Text.Clean(request.BasisNote, 500));

            _log.Add(templateId is null ? "TASK_TEMPLATE_CREATE" : "TASK_TEMPLATE_UPDATE", $"مهمة {template.Code}: {Short(title)}");
            await _unitOfWork.SaveChangesAsync(ct);
            return template.Id;
        }

        public async Task DeleteTemplateAsync(long templateId, CancellationToken ct = default)
        {
            _currentUser.RequireAdmin();
            var template = await _scope.GetTemplateForUpdateAsync(templateId, ct)
                ?? throw new NotFoundException("المهمة المطلوبة غير موجودة.");
            if (await _scope.IsTemplateUsedAsync(templateId, ct))
                throw new BusinessRuleException("هذه المهمة مستخدمة في نطاق عمل تقييمات حالية ولا يمكن حذفها حفاظًا على سجل التنفيذ. يمكنك إيقافها بدلًا من الحذف.");

            var rules = await _scope.GetRulesForTemplateAsync(templateId, ct);
            foreach (var rule in rules) _scope.RemoveRule(rule);
            _scope.RemoveTemplate(template);

            _log.Add("TASK_TEMPLATE_DELETE", $"حذف مهمة {template.Code} و{rules.Count} تأثير مرتبط بها");
            await _unitOfWork.SaveChangesAsync(ct);
        }

        // ------------------------------------------------------------------ rules

        public async Task<long> SaveRuleAsync(long? ruleId, SaveRuleRequest request, CancellationToken ct = default)
        {
            _currentUser.RequireAdmin();
            var questions = (await PublishedQuestionsAsync(ct)).ToDictionary(q => q.Code);

            var questionCode = Text.Clean(request.QuestionCode, 16).ToUpperInvariant();
            if (!questions.TryGetValue(questionCode, out var question))
                throw new BusinessRuleException($"السؤال {questionCode} غير موجود في بنك الأسئلة المنشور.");

            var value = ValidateCondition(question, request.Operator, request.Value);

            TaskTemplate? target = null;
            if (request.EffectType is RuleEffectType.ExpandTask or RuleEffectType.AddProcedure or RuleEffectType.OptionalService)
            {
                if (request.TaskTemplateId is not long targetId) throw new BusinessRuleException("اختاري المهمة التي يؤثر عليها هذا الشرط.");
                target = await _scope.GetTemplateForUpdateAsync(targetId, ct)
                    ?? throw new BusinessRuleException("المهمة المختارة غير موجودة.");
            }

            var recommendation = Text.CleanMultiline(request.Recommendation, 2000);
            if (request.EffectType == RuleEffectType.Recommendation && recommendation.Length == 0)
                throw new BusinessRuleException("اكتبي نص التوصية.");

            AnswerRule rule;
            if (ruleId is null)
            {
                var code = Text.Clean(request.Code, 20).ToUpperInvariant();
                // Time alone collides when two rules are saved in the same second; the suffix keeps it unique.
                if (code.Length == 0)
                    code = "R-" + _clock.GetUtcNow().ToString("yyMMddHHmm", CultureInfo.InvariantCulture)
                         + "-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
                if (!CodePattern.IsMatch(code)) throw new BusinessRuleException("كود القاعدة يقبل الحروف الإنجليزية الكبيرة والأرقام والشرطة فقط.");
                if (await _scope.RuleCodeExistsAsync(code, ct)) throw new BusinessRuleException($"كود القاعدة {code} مستخدم بالفعل.");
                rule = new AnswerRule { Code = code };
                _scope.AddRule(rule);
            }
            else
            {
                rule = await _scope.GetRuleForUpdateAsync(ruleId.Value, ct)
                    ?? throw new NotFoundException("القاعدة المطلوبة غير موجودة.");
            }

            rule.QuestionCode = questionCode;
            rule.Operator = request.Operator;
            rule.Value = value;
            rule.EffectType = request.EffectType;
            rule.TaskTemplate = target;
            rule.TaskTemplateId = target?.Id;
            rule.RoleDelta = request.EffectType == RuleEffectType.ExpandTask
                ? ValidateHours(request.RoleDelta ?? new RoleHoursInput(0, 0, 0, 0), "الساعات الإضافية")
                : new RoleHours();
            rule.Recommendation = request.EffectType == RuleEffectType.Recommendation ? recommendation : null;
            rule.ConditionNote = NullIfEmpty(Text.CleanMultiline(request.ConditionNote, 2000));
            rule.Priority = request.Priority;
            rule.IsActive = request.IsActive;

            _log.Add(ruleId is null ? "ANSWER_RULE_CREATE" : "ANSWER_RULE_UPDATE", $"قاعدة {rule.Code}: {questionCode} ← {Labels.Of(rule.EffectType)}");
            await _unitOfWork.SaveChangesAsync(ct);
            return rule.Id;
        }

        public async Task DeleteRuleAsync(long ruleId, CancellationToken ct = default)
        {
            _currentUser.RequireAdmin();
            var rule = await _scope.GetRuleForUpdateAsync(ruleId, ct)
                ?? throw new NotFoundException("القاعدة المطلوبة غير موجودة.");
            _scope.RemoveRule(rule);
            _log.Add("ANSWER_RULE_DELETE", $"حذف قاعدة {rule.Code}");
            await _unitOfWork.SaveChangesAsync(ct);
        }

        // ------------------------------------------------------------------ pricing

        public async Task SavePricingAsync(SavePricingRequest request, CancellationToken ct = default)
        {
            _currentUser.RequireAdmin();
            foreach (var rate in new[] { request.HourlyRates.Analyst, request.HourlyRates.TeamLead, request.HourlyRates.Manager, request.HourlyRates.Partner })
                if (rate < 0 || rate > 1_000_000) throw new BusinessRuleException("معدل الساعة يجب أن يكون بين 0 و1,000,000.");
            if (request.MarginPct < 0 || request.MarginPct > 500) throw new BusinessRuleException("هامش التسعير يجب أن يكون بين 0% و500%.");

            var settings = await _scope.GetPricingForUpdateAsync(ct);
            settings.HourlyRates = new RoleHours
            {
                Analyst = request.HourlyRates.Analyst, TeamLead = request.HourlyRates.TeamLead,
                Manager = request.HourlyRates.Manager, Partner = request.HourlyRates.Partner
            };
            settings.MarginPct = request.MarginPct;
            settings.MethodologyStatus = request.MethodologyStatus;
            settings.MethodologyNote = NullIfEmpty(Text.CleanMultiline(request.MethodologyNote, 2000));

            _log.Add("PRICING_UPDATE", $"تحديث معدلات الأدوار؛ الهامش {request.MarginPct:0.##}%");
            await _unitOfWork.SaveChangesAsync(ct);
        }

        // ------------------------------------------------------------------ helpers

        private async Task<List<QuestionChoice>> PublishedQuestionsAsync(CancellationToken ct)
        {
            var version = await _questionBank.GetPublishedVersionAsync(ct);
            if (version is null) return new List<QuestionChoice>();
            return version.Categories.OrderBy(c => c.SortOrder)
                .SelectMany(c => c.Questions.OrderBy(q => q.SortOrder))
                .Select(q => new QuestionChoice(q.Code, q.Text, q.IsNumeric, q.AnswerType == AnswerType.Multi,
                    q.Options.OrderBy(o => o.SortOrder).Select(o => o.Text).ToList()))
                .ToList();
        }

        /// <summary>
        /// Checks that the condition can ever be true: a value where one is needed, numbers for numeric
        /// comparisons, and listed options for equals / in on choice questions.
        /// </summary>
        private static string? ValidateCondition(QuestionChoice question, RuleOperator op, string? rawValue)
        {
            var value = Text.Clean(rawValue, 500);
            switch (op)
            {
                case RuleOperator.Answered:
                case RuleOperator.NotAnswered:
                    return null;
                case RuleOperator.GreaterThan:
                case RuleOperator.GreaterOrEqual:
                case RuleOperator.LessThan:
                    if (!NumberParser.TryParse(value, out _)) throw new BusinessRuleException("المقارنة الرقمية تحتاج قيمة رقمية.");
                    return value;
            }

            if (value.Length == 0) throw new BusinessRuleException("اكتبي قيمة الشرط.");
            if (op == RuleOperator.Contains || question.Options.Count == 0) return value;

            var values = op == RuleOperator.In
                ? value.Split('|').Select(v => v.Trim()).Where(v => v.Length > 0).ToList()
                : new List<string> { value };
            var unknown = values.Where(v => !question.Options.Contains(v)).ToList();
            if (unknown.Count > 0)
                throw new BusinessRuleException($"القيمة «{string.Join("، ", unknown)}» ليست من اختيارات السؤال {question.Code}.");
            return op == RuleOperator.In ? string.Join("|", values) : value;
        }

        private static RoleHours ValidateHours(RoleHoursInput input, string label)
        {
            foreach (var h in new[] { input.Analyst, input.TeamLead, input.Manager, input.Partner })
                if (h < 0 || h > MaxRoleHours) throw new BusinessRuleException($"{label} لكل دور يجب أن تكون بين 0 و{MaxRoleHours:0}.");
            return new RoleHours { Analyst = input.Analyst, TeamLead = input.TeamLead, Manager = input.Manager, Partner = input.Partner };
        }

        private static RoleHoursInput ToInput(RoleHours h) => new(h.Analyst, h.TeamLead, h.Manager, h.Partner);

        private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;

        private static string Short(string text) => text.Length > 80 ? text[..80] + "…" : text;
    }
}
