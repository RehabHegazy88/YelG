using YelG.BLL.Scoring;
using YelG.BLL.ViewModel;

namespace YelG.AdminPortal.Models
{
    public sealed record HomeViewModel(IReadOnlyList<EvaluationListItem> MyEvaluations, int PendingApproval);

    public sealed record CompaniesViewModel(
        IReadOnlyList<CompanyListItem> Companies, IReadOnlyList<UserListItem> Users, IReadOnlyList<ActivityItem> Activities);

    public sealed record MyEvaluationsViewModel(IReadOnlyList<EvaluationListItem> Evaluations, IReadOnlyList<ActivityItem> Activities);

    /// <param name="Initial">Visibility and score of the saved answers, so the page renders without a flash.</param>
    public sealed record EvaluationPageViewModel(EvaluationDetails Evaluation, QuestionBankForm Form, EvaluationPreview Initial);

    public sealed record QuestionBankPageViewModel(QuestionBankOverview Overview, QuestionBankEditor Editor);

    /// <param name="Price">First-year price, or null while rates are missing or some task has no hours.</param>
    public sealed record ScopePageViewModel(EvaluationDetails Evaluation, ScopeResult Scope, bool CanEdit, decimal? Price);

    /// <summary>Posted by the company forms (create / edit / reviewers).</summary>
    public sealed class CompanyForm
    {
        public string Name { get; set; } = string.Empty;
        public long ActivityId { get; set; }
        public List<long> ReviewerIds { get; set; } = new();
    }

    /// <summary>Posted by the user form (create / edit).</summary>
    public sealed class UserForm
    {
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Password { get; set; }
    }

    /// <summary>JSON body for preview and save on the evaluation screen.</summary>
    public sealed class EvaluationInput
    {
        public Dictionary<string, List<string>> Answers { get; set; } = new();
        public YelG.Model.Enums.ConfidenceGrade? ProfessionalJudgment { get; set; }
        public string? Note { get; set; }
        public string? RowVersion { get; set; }

        public IReadOnlyDictionary<string, IReadOnlyList<string>> AnswersMap() =>
            Answers.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)(p.Value ?? new List<string>()));
    }

    public sealed class RowVersionInput
    {
        public string? RowVersion { get; set; }
    }
}
