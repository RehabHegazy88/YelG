using System.Globalization;
using YelG.Model.Enums;
using YelG.Model.Scope;

namespace YelG.BLL.Scoring
{
    /// <summary>What people set on a generated task (status, owner, date). Defaults when no row exists yet.</summary>
    public sealed record ScopeItemState(
        ScopeItemStatus Status,
        TaskOwner Owner,
        DateOnly? DueDate,
        string? EvidenceNote,
        bool OptionalIncluded)
    {
        public static ScopeItemState DefaultFor(TaskTemplate template) =>
            new(ScopeItemStatus.Planned, template.DefaultOwner, null, null, false);

        public static ScopeItemState From(ScopeItem item) =>
            new(item.Status, item.Owner, item.DueDate, item.EvidenceNote, item.OptionalIncluded);
    }

    public sealed class ScopeTask
    {
        public TaskTemplate Template { get; init; } = null!;
        public TaskPriority Priority { get; init; }
        public IReadOnlyList<string> Sources { get; init; } = Array.Empty<string>();
        public RoleHours RoleDelta { get; init; } = new();
        public RoleHours RoleHours { get; init; } = new();
        public decimal Hours { get; init; }
        public int VolumeFactor { get; init; }
        public string VolumeLabel { get; init; } = string.Empty;
        public int AnnualMultiplier { get; init; }
        public RoleHours AnnualRoleHours { get; init; } = new();
        public decimal AnnualHours { get; init; }
        public ScopeItemState State { get; init; } = null!;
    }

    public sealed record ScopeRecommendation(string RuleCode, string Text, TaskPriority Priority, string Source);

    public sealed record ScopeOptionalService(AnswerRule Rule, TaskTemplate Template, ScopeItemState State, string Source);

    public sealed record ScopeRuleHit(AnswerRule Rule, string Source);

    public sealed class ScopeResult
    {
        public IReadOnlyList<ScopeTask> Tasks { get; init; } = Array.Empty<ScopeTask>();
        public IReadOnlyList<ScopeRecommendation> Recommendations { get; init; } = Array.Empty<ScopeRecommendation>();
        public IReadOnlyList<ScopeOptionalService> OptionalServices { get; init; } = Array.Empty<ScopeOptionalService>();
        public IReadOnlyList<ScopeRuleHit> ScoreEffects { get; init; } = Array.Empty<ScopeRuleHit>();
    }

    /// <summary>
    /// Port of generateCompanyScope(), ruleMatches(), estimateTaskWorkload() and numericValue().
    /// </summary>
    public static class ScopeEngine
    {
        public const string BaseProgramSource = "برنامج المراجعة الأساسي";
        private const string ExpandPrefix = "توسيع إجراء قائم: ";
        private const string AddPrefix = "إجراء ناتج عن الإجابة: ";
        private const string DefaultRecommendation = "مراجعة الملاحظة ووضع إجراء تصحيحي.";
        private const string AnsweredLabel = "تمت الإجابة";

        /// <summary>
        /// Only answers to active, visible questions take part. A stale answer to a question that was
        /// hidden by a later change to its parent no longer triggers rules or scales hours
        /// (generateCompanyScope() in the front end used every stored answer).
        /// </summary>
        /// <param name="states">Saved state keyed by (template id, is optional service row).</param>
        public static ScopeResult Generate(
            ScoringModel model,
            AnswerSet allAnswers,
            IReadOnlyList<TaskTemplate> templates,
            IReadOnlyList<AnswerRule> rules,
            IReadOnlyDictionary<(long TemplateId, bool Optional), ScopeItemState> states)
        {
            var answers = VisibleAnswers(model, allAnswers);
            var questionTexts = model.QuestionsByCode.ToDictionary(p => p.Key, p => p.Value.Text, StringComparer.Ordinal);

            var activeTemplates = templates.Where(t => t.IsActive).ToDictionary(t => t.Id);
            var selected = new List<Selection>();
            var selectedById = new Dictionary<long, Selection>();
            var recommendations = new List<ScopeRecommendation>();
            var optionalServices = new List<ScopeOptionalService>();
            var scoreEffects = new List<ScopeRuleHit>();

            foreach (var template in templates.Where(t => t.IsActive && t.DefaultIncluded))
            {
                var selection = new Selection(template, template.Priority);
                selection.Sources.Add(BaseProgramSource);
                selected.Add(selection);
                selectedById[template.Id] = selection;
            }

            foreach (var rule in rules.Where(r => RuleMatches(r, answers)))
            {
                var source = RuleSource(rule, questionTexts);

                if (rule.EffectType == RuleEffectType.ScoreOnly)
                {
                    scoreEffects.Add(new ScopeRuleHit(rule, source));
                    continue;
                }
                if (rule.EffectType == RuleEffectType.Recommendation)
                {
                    var text = string.IsNullOrWhiteSpace(rule.Recommendation) ? DefaultRecommendation : rule.Recommendation!;
                    recommendations.Add(new ScopeRecommendation(rule.Code, text, rule.Priority, source));
                    continue;
                }
                if (rule.TaskTemplateId is not long templateId || !activeTemplates.TryGetValue(templateId, out var target))
                    continue;

                if (rule.EffectType == RuleEffectType.OptionalService)
                {
                    var optionalState = StateOf(states, target, optional: true);
                    optionalServices.Add(new ScopeOptionalService(rule, target, optionalState, source));
                    if (!optionalState.OptionalIncluded) continue;
                }

                if (!selectedById.TryGetValue(target.Id, out var current))
                {
                    current = new Selection(target, rule.Priority);
                    selected.Add(current);
                    selectedById[target.Id] = current;
                }

                var expand = rule.EffectType == RuleEffectType.ExpandTask;
                current.Sources.Add((expand ? ExpandPrefix : AddPrefix) + source);
                if (expand) AddInto(current.RoleDelta, rule.RoleDelta, 1m);
                if (rule.Priority == TaskPriority.High) current.Priority = TaskPriority.High;
            }

            var tasks = selected.Select(s => BuildTask(s, answers, questionTexts, StateOf(states, s.Template, optional: false))).ToList();

            return new ScopeResult
            {
                Tasks = tasks,
                Recommendations = recommendations,
                OptionalServices = optionalServices,
                ScoreEffects = scoreEffects
            };
        }

        /// <summary>Answers of active questions that are currently visible under the branching rules.</summary>
        public static AnswerSet VisibleAnswers(ScoringModel model, AnswerSet answers) =>
            AnswerSet.From(answers.ToDictionary()
                .Where(p => model.QuestionsByCode.TryGetValue(p.Key, out var question)
                            && ScoringEngine.IsVisible(model, question, answers)));

        public static bool RuleMatches(AnswerRule rule, AnswerSet answers)
        {
            if (!rule.IsActive) return false;
            var values = answers.ValuesOf(rule.QuestionCode);
            var expected = rule.Value ?? string.Empty;

            return rule.Operator switch
            {
                RuleOperator.Answered => values.Count > 0,
                RuleOperator.NotAnswered => values.Count == 0,
                RuleOperator.Contains => values.Any(v => v.Contains(expected, StringComparison.Ordinal)),
                RuleOperator.In => expected.Split('|')
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .Any(x => values.Contains(x, StringComparer.Ordinal)),
                RuleOperator.NotEquals => !values.Contains(expected, StringComparer.Ordinal),
                RuleOperator.GreaterThan => NumericValue(values) > NumericValue(expected),
                RuleOperator.GreaterOrEqual => NumericValue(values) >= NumericValue(expected),
                RuleOperator.LessThan => NumericValue(values) < NumericValue(expected),
                _ => values.Contains(expected, StringComparer.Ordinal)
            };
        }

        public static int FrequencyMultiplier(TaskFrequency frequency) => frequency switch
        {
            TaskFrequency.Weekly => 52,
            TaskFrequency.BiWeekly => 26,
            TaskFrequency.Monthly => 12,
            TaskFrequency.Quarterly => 4,
            TaskFrequency.SemiAnnual => 2,
            _ => 1
        };

        /// <summary>Price = Σ(role hours × role rate) × (1 + margin). Null until at least one rate is set.</summary>
        /// <summary>
        /// First-year price of the tasks, or null while rates are not set or some task has no hours yet
        /// (a partial price would look final when it is not).
        /// </summary>
        public static decimal? FirstYearPrice(IReadOnlyList<ScopeTask> tasks, PricingSettings settings)
        {
            if (tasks.Count == 0 || tasks.Any(t => t.Hours == 0)) return null;
            var firstYear = new RoleHours
            {
                Analyst = tasks.Sum(t => t.AnnualRoleHours.Analyst),
                TeamLead = tasks.Sum(t => t.AnnualRoleHours.TeamLead),
                Manager = tasks.Sum(t => t.AnnualRoleHours.Manager),
                Partner = tasks.Sum(t => t.AnnualRoleHours.Partner)
            };
            return Price(firstYear, settings);
        }

        public static decimal? Price(RoleHours hours, PricingSettings settings)
        {
            var rates = settings.HourlyRates;
            if (rates.Analyst <= 0 && rates.TeamLead <= 0 && rates.Manager <= 0 && rates.Partner <= 0) return null;
            var cost = rates.Analyst * hours.Analyst + rates.TeamLead * hours.TeamLead
                     + rates.Manager * hours.Manager + rates.Partner * hours.Partner;
            return cost * (1 + settings.MarginPct / 100m);
        }

        /// <summary>
        /// First number in the text, after converting Arabic-Indic digits and removing thousands separators
        /// (both "," and the Arabic "٬"); the Arabic decimal mark "٫" reads as ".". 0 when there is no number.
        /// The front end's numericValue() missed "٬", so "١٢٬٥٠٠٬٠٠٠" was read as 12.
        /// </summary>
        public static double NumericValue(IReadOnlyList<string> values) =>
            values.Count == 0 ? 0 : NumericValue(values[0]);

        public static double NumericValue(string? value) => NumberParser.ValueOrZero(value);

        private static ScopeTask BuildTask(Selection selection, AnswerSet answers,
            IReadOnlyDictionary<string, string> questionTexts, ScopeItemState state)
        {
            var template = selection.Template;
            var (factor, label) = VolumeFactor(template, answers, questionTexts);

            var roleHours = new RoleHours();
            AddInto(roleHours, template.RoleHours, factor);
            AddInto(roleHours, selection.RoleDelta, 1m);
            roleHours.Analyst = RoundHalf(roleHours.Analyst);
            roleHours.TeamLead = RoundHalf(roleHours.TeamLead);
            roleHours.Manager = RoundHalf(roleHours.Manager);
            roleHours.Partner = RoundHalf(roleHours.Partner);

            var multiplier = FrequencyMultiplier(template.Frequency);
            var annual = new RoleHours();
            AddInto(annual, roleHours, multiplier);

            return new ScopeTask
            {
                Template = template,
                Priority = selection.Priority,
                Sources = selection.Sources.Distinct(StringComparer.Ordinal).ToList(),
                RoleDelta = selection.RoleDelta,
                RoleHours = roleHours,
                Hours = roleHours.Total,
                VolumeFactor = factor,
                VolumeLabel = label,
                AnnualMultiplier = multiplier,
                AnnualRoleHours = annual,
                AnnualHours = roleHours.Total * multiplier,
                State = state
            };
        }

        private static (int Factor, string Label) VolumeFactor(TaskTemplate template, AnswerSet answers,
            IReadOnlyDictionary<string, string> questionTexts)
        {
            if (string.IsNullOrEmpty(template.DriverQuestionCode)) return (1, "حجم ثابت");

            var amount = NumericValue(answers.ValuesOf(template.DriverQuestionCode));
            var unit = Math.Max(1d, (double)template.DriverUnit);
            var max = Math.Max(1, template.MaxMultiplier);
            var factor = amount > 0 ? Math.Max(1, Math.Min(max, (int)Math.Ceiling(amount / unit))) : 1;

            var name = questionTexts.TryGetValue(template.DriverQuestionCode, out var text) ? text : template.DriverQuestionCode;
            return (factor, name + " · كل " + unit.ToString(CultureInfo.InvariantCulture) + " وحدة");
        }

        private static string RuleSource(AnswerRule rule, IReadOnlyDictionary<string, string> questionTexts)
        {
            var question = questionTexts.TryGetValue(rule.QuestionCode, out var text)
                ? rule.QuestionCode + " · " + text
                : rule.QuestionCode;
            return question + " ← " + (string.IsNullOrEmpty(rule.Value) ? AnsweredLabel : rule.Value);
        }

        private static ScopeItemState StateOf(IReadOnlyDictionary<(long, bool), ScopeItemState> states, TaskTemplate template, bool optional) =>
            states.TryGetValue((template.Id, optional), out var state) ? state : ScopeItemState.DefaultFor(template);

        private static void AddInto(RoleHours target, RoleHours? extra, decimal multiplier)
        {
            if (extra is null) return;
            target.Analyst += extra.Analyst * multiplier;
            target.TeamLead += extra.TeamLead * multiplier;
            target.Manager += extra.Manager * multiplier;
            target.Partner += extra.Partner * multiplier;
        }

        /// <summary>Math.round(x * 2) / 2 in JavaScript (half rounds up).</summary>
        private static decimal RoundHalf(decimal value) =>
            Math.Floor(value * 2 + 0.5m) / 2;

        private sealed class Selection
        {
            public Selection(TaskTemplate template, TaskPriority priority)
            {
                Template = template;
                Priority = priority;
            }

            public TaskTemplate Template { get; }
            public TaskPriority Priority { get; set; }
            public List<string> Sources { get; } = new();
            public RoleHours RoleDelta { get; } = new();
        }
    }
}
