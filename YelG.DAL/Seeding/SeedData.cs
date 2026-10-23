using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using YelG.Model.Enums;
using YelG.Model.QuestionBank;
using YelG.Model.Scope;

namespace YelG.DAL.Seeding
{
    /// <summary>
    /// Reads the embedded seed files (Seeding/Data/*.json) generated from the Apps Script data
    /// and turns them into entity graphs. Public so tests can build the same model without a DB.
    /// </summary>
    public static class SeedData
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static QuestionBankVersion LoadQuestionBank()
        {
            var bank = Read<BankDto>("question-bank.json");
            var version = new QuestionBankVersion
            {
                VersionNo = bank.VersionNo,
                Status = QuestionBankVersionStatus.Published,
                ReleaseNotes = bank.Notes
            };

            var byCode = new Dictionary<string, Question>(StringComparer.Ordinal);
            var parents = new List<(Question Child, string ParentCode)>();

            foreach (var c in bank.Categories)
            {
                var category = new Category
                {
                    Code = c.Code,
                    Title = c.Title,
                    Status = c.Status,
                    SortOrder = c.SortOrder,
                    BaseWeight = c.BaseWeight
                };

                foreach (var q in c.Questions)
                {
                    var question = new Question
                    {
                        Code = q.Code,
                        Text = q.Text,
                        SortOrder = q.SortOrder,
                        AnswerType = q.AnswerType,
                        Kind = q.Kind,
                        Direction = q.Direction,
                        IsCritical = q.IsCritical,
                        AllowCustomAnswer = q.AllowCustomAnswer,
                        IsActive = q.IsActive,
                        WeightConfidence = q.Weights[0],
                        WeightWorkflow = q.Weights[1],
                        WeightPricing = q.Weights[2],
                        ConditionOperator = q.ConditionOperator,
                        ConditionValue = q.ConditionValue,
                        Options = q.Options.Select(o => new QuestionOption
                        {
                            Text = o.Text,
                            SortOrder = o.SortOrder,
                            Score = o.Score
                        }).ToList(),
                        ScoreBands = (q.ScoreBands ?? new List<BandDto>()).Select((band, i) => new QuestionScoreBand
                        {
                            MinValue = band.MinValue,
                            MaxValue = band.MaxValue,
                            Score = band.Score,
                            SortOrder = i + 1
                        }).ToList()
                    };
                    category.Questions.Add(question);
                    byCode[question.Code] = question;
                    if (!string.IsNullOrEmpty(q.ParentCode)) parents.Add((question, q.ParentCode));
                }

                version.Categories.Add(category);
            }

            foreach (var (child, parentCode) in parents)
                child.ParentQuestion = byCode.TryGetValue(parentCode, out var parent)
                    ? parent
                    : throw new InvalidOperationException($"Seed question {child.Code} points to missing parent {parentCode}.");

            return version;
        }

        /// <summary>Templates and rules; rules are linked to templates through navigation properties.</summary>
        public static (List<TaskTemplate> Templates, List<AnswerRule> Rules) LoadTaskLibrary()
        {
            var library = Read<LibraryDto>("task-library.json");

            var templates = library.Templates.Select(t => new TaskTemplate
            {
                Code = t.Code,
                Title = t.Title,
                ServiceLine = t.ServiceLine,
                Evidence = t.Evidence,
                Purpose = t.Purpose,
                DefaultOwner = t.DefaultOwner,
                SourceQuestionCode = t.SourceQuestionCode,
                RoleHours = t.RoleHours.ToEntity(),
                DriverQuestionCode = t.DriverQuestionCode,
                DriverUnit = t.DriverUnit,
                MaxMultiplier = t.MaxMultiplier,
                Frequency = t.Frequency,
                Priority = t.Priority,
                DefaultIncluded = t.DefaultIncluded,
                IsActive = t.IsActive,
                BasisStatus = t.BasisStatus,
                BasisNote = t.BasisNote
            }).ToList();

            var templateByCode = templates.ToDictionary(t => t.Code, StringComparer.Ordinal);

            var rules = library.Rules.Select(r => new AnswerRule
            {
                Code = r.Code,
                QuestionCode = r.QuestionCode,
                Operator = r.Operator,
                Value = r.Value,
                EffectType = r.EffectType,
                TaskTemplate = r.TemplateCode is null ? null
                    : templateByCode.TryGetValue(r.TemplateCode, out var template) ? template
                    : throw new InvalidOperationException($"Seed rule {r.Code} points to missing task {r.TemplateCode}."),
                Recommendation = r.Recommendation,
                RoleDelta = r.RoleDelta.ToEntity(),
                Priority = r.Priority,
                ConditionNote = r.ConditionNote,
                IsActive = r.IsActive
            }).ToList();

            return (templates, rules);
        }

        private static T Read<T>(string fileName)
        {
            var assembly = typeof(SeedData).Assembly;
            var resource = assembly.GetManifestResourceNames().SingleOrDefault(n => n.EndsWith("." + fileName, StringComparison.Ordinal))
                ?? throw new InvalidOperationException($"Embedded seed file {fileName} was not found.");
            using var stream = assembly.GetManifestResourceStream(resource)!;
            return JsonSerializer.Deserialize<T>(stream, Options)
                ?? throw new InvalidOperationException($"Seed file {fileName} is empty.");
        }

        private sealed record BankDto(int VersionNo, string? Notes, List<CategoryDto> Categories);

        private sealed record CategoryDto(string Code, string Title, string Status, int SortOrder, decimal BaseWeight, List<QuestionDto> Questions);

        private sealed record QuestionDto(
            string Code, string Text, int SortOrder, AnswerType AnswerType, QuestionKind Kind, QuestionDirection Direction,
            bool IsCritical, bool AllowCustomAnswer, bool IsActive, decimal[] Weights,
            string? ParentCode, ConditionOperator? ConditionOperator, string? ConditionValue, List<OptionDto> Options,
            List<BandDto>? ScoreBands = null);

        private sealed record OptionDto(string Text, int SortOrder, decimal? Score);

        private sealed record BandDto(decimal? MinValue, decimal? MaxValue, decimal Score);

        private sealed record LibraryDto(List<TemplateDto> Templates, List<RuleDto> Rules);

        private sealed record RoleHoursDto(decimal Analyst, decimal TeamLead, decimal Manager, decimal Partner)
        {
            public RoleHours ToEntity() => new() { Analyst = Analyst, TeamLead = TeamLead, Manager = Manager, Partner = Partner };
        }

        private sealed record TemplateDto(
            string Code, string Title, string? ServiceLine, string? Evidence, string? Purpose, TaskOwner DefaultOwner,
            string? SourceQuestionCode, RoleHoursDto RoleHours, string? DriverQuestionCode, decimal DriverUnit, int MaxMultiplier,
            TaskFrequency Frequency, TaskPriority Priority, bool DefaultIncluded, bool IsActive, BasisStatus BasisStatus, string? BasisNote);

        private sealed record RuleDto(
            string Code, string QuestionCode, RuleOperator Operator, string? Value, RuleEffectType EffectType, string? TemplateCode,
            string? Recommendation, RoleHoursDto RoleDelta, TaskPriority Priority, string? ConditionNote, bool IsActive);
    }
}
