using System.Text.Json;
using System.Text.Json.Serialization;
using YelG.BLL.Scoring;
using YelG.DAL.Seeding;
using YelG.Model.Enums;
using YelG.Model.Scope;

namespace YelG.BLL.Tests.Parity
{
    /// <summary>
    /// Golden cases produced by running the original Index.html functions (score, generateCompanyScope)
    /// in node on the same seed data. See parity-cases.json.
    /// </summary>
    public static class ParityCases
    {
        /// <summary>CATW default in the front end: +(100 / MODEL.length).toFixed(1).</summary>
        private const double JsDefaultCategoryWeight = 7.7;

        private static readonly Lazy<ParityFile> File = new(() =>
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Parity", "parity-cases.json");
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<ParityFile>(System.IO.File.ReadAllText(path), options)!;
        });

        private static readonly Lazy<ScoringModel> SeedModel = new(() => SeedData.LoadQuestionBank().ToScoringModel());

        public static IEnumerable<object[]> Names() => File.Value.Cases.Select(c => new object[] { c.Name });

        public static ParityCase Get(string name) => File.Value.Cases.Single(c => c.Name == name);

        public static ScoringModel ModelFor(ParityCase parityCase)
        {
            var categories = SeedModel.Value.Categories.Select(c =>
            {
                var weight = parityCase.CatWeights is not null && parityCase.CatWeights.TryGetValue(c.Code, out var w)
                    ? w
                    : JsDefaultCategoryWeight;
                var factors = parityCase.Factors is not null && parityCase.Factors.TryGetValue(c.Code, out var f)
                    ? new Dictionary<long, double> { [parityCase.ActivityId] = f }
                    : new Dictionary<long, double>();
                return new ScoringCategory(c.Code, c.Title, weight, factors, c.Questions);
            }).ToList();
            return new ScoringModel(0, categories);
        }

        public static AnswerSet AnswersFor(ParityCase parityCase) =>
            new(parityCase.Answers.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value));

        /// <summary>Seeded library plus the synthetic templates/rules the golden run added.</summary>
        public static (List<TaskTemplate> Templates, List<AnswerRule> Rules) TaskLibrary()
        {
            var (templates, rules) = SeedData.LoadTaskLibrary();

            foreach (var t in File.Value.ExtraTemplates)
            {
                templates.Add(new TaskTemplate
                {
                    Code = t.Code,
                    Title = t.Title,
                    DefaultIncluded = t.DefaultIncluded,
                    IsActive = t.IsActive,
                    DefaultOwner = t.DefaultOwner,
                    RoleHours = t.RoleHours.ToEntity(),
                    DriverQuestionCode = t.DriverQuestionCode,
                    DriverUnit = t.DriverUnit,
                    MaxMultiplier = t.MaxMultiplier,
                    Frequency = t.Frequency,
                    Priority = t.Priority
                });
            }
            for (var i = 0; i < templates.Count; i++) templates[i].Id = i + 1;
            var byCode = templates.ToDictionary(t => t.Code);

            foreach (var r in File.Value.ExtraRules)
            {
                rules.Add(new AnswerRule
                {
                    Code = r.Code,
                    QuestionCode = r.QuestionCode,
                    Operator = r.Operator,
                    Value = r.Value,
                    EffectType = r.EffectType,
                    TaskTemplate = r.TemplateCode is null ? null : byCode[r.TemplateCode],
                    RoleDelta = r.RoleDelta?.ToEntity() ?? new RoleHours(),
                    Recommendation = r.Recommendation,
                    Priority = r.Priority,
                    IsActive = r.IsActive
                });
            }
            for (var i = 0; i < rules.Count; i++)
            {
                rules[i].Id = i + 1;
                rules[i].TaskTemplateId = rules[i].TaskTemplate?.Id;
            }

            return (templates, rules);
        }
    }

    public sealed record ParityFile(List<ExtraTemplate> ExtraTemplates, List<ExtraRule> ExtraRules, List<ParityCase> Cases);

    public sealed record RoleHoursDto(decimal Analyst, decimal TeamLead, decimal Manager, decimal Partner)
    {
        public RoleHours ToEntity() => new() { Analyst = Analyst, TeamLead = TeamLead, Manager = Manager, Partner = Partner };
    }

    public sealed record ExtraTemplate(
        string Code, string Title, bool DefaultIncluded, bool IsActive, TaskOwner DefaultOwner, RoleHoursDto RoleHours,
        string? DriverQuestionCode, decimal DriverUnit, int MaxMultiplier, TaskFrequency Frequency, TaskPriority Priority);

    public sealed record ExtraRule(
        string Code, string QuestionCode, RuleOperator Operator, string? Value, RuleEffectType EffectType,
        string? TemplateCode, RoleHoursDto? RoleDelta, string? Recommendation, TaskPriority Priority, bool IsActive);

    public sealed record ParityCase(
        string Name, long ActivityId, Dictionary<string, double>? CatWeights, Dictionary<string, double>? Factors,
        List<string> OptionalIncluded, Dictionary<string, List<string>> Answers, ExpectedResult Expected);

    public sealed record ExpectedResult(ExpectedScore Score, ExpectedScope Scope);

    public sealed record ExpectedScore(
        double Confidence, ConfidenceGrade Grade, int Answered, int VisibleCount,
        List<string> Breaches, List<ExpectedCategory> Categories);

    public sealed record ExpectedCategory(string Code, double? Confidence, double? Workflow, double? Pricing, double RelativeWeight);

    public sealed record ExpectedScope(
        List<ExpectedTask> Tasks, List<ExpectedRecommendation> Recommendations,
        List<ExpectedOptionalService> OptionalServices, List<string> ScoreEffects);

    public sealed record ExpectedTask(
        string Code, TaskPriority Priority, List<string> Sources, RoleHoursDto RoleHours, decimal Hours,
        int VolumeFactor, string VolumeLabel, int AnnualMultiplier, decimal AnnualHours, TaskOwner Owner, ScopeItemStatus Status);

    public sealed record ExpectedRecommendation(string Code, string Text, TaskPriority Priority, string Source);

    public sealed record ExpectedOptionalService(string RuleCode, string TemplateCode, bool Included, string Source);
}
