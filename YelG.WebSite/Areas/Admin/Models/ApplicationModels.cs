using YelG.WebSite.Domain.Sections;
using System.Globalization;
using YelG.WebSite.Domain.Careers;

namespace YelG.WebSite.Areas.Admin.Models
{
    /// <summary>The applications inbox filters, from the query string.</summary>
    public sealed record ApplicationFilter
    {
        public ApplicationStatus? Status { get; init; }

        /// <summary>A role id.</summary>
        public string? Role { get; init; }

        /// <summary>Searches the name and email.</summary>
        public string? Q { get; init; }

        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }

        /// <summary>
        /// Answers to the selected role's extra questions, by question id (?a[12]=yes): "yes" / "no", one option
        /// (Arabic), or words a text answer contains. Ignored for questions of other roles.
        /// </summary>
        public Dictionary<long, string>? A { get; init; }

        /// <summary>The answer filter value for a question, or null.</summary>
        public string? Answer(long questionId) => A is not null && A.TryGetValue(questionId, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

        public string Query(int? page = null) => string.Join("&", new[]
        {
            Status is null ? null : $"status={Status}",
            string.IsNullOrEmpty(Role) ? null : "role=" + Uri.EscapeDataString(Role),
            string.IsNullOrWhiteSpace(Q) ? null : "q=" + Uri.EscapeDataString(Q),
            From is null ? null : "from=" + From.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            To is null ? null : "to=" + To.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            A is null ? null : string.Join("&", A.Where(x => !string.IsNullOrWhiteSpace(x.Value)).OrderBy(x => x.Key)
                .Select(x => $"a%5B{x.Key.ToString(CultureInfo.InvariantCulture)}%5D=" + Uri.EscapeDataString(x.Value.Trim()))) is { Length: > 0 } answers ? answers : null,
            page is > 1 ? $"page={page}" : null
        }.Where(x => x is not null));

        public override string ToString() => Query();
    }

    public sealed class ApplicationListModel
    {
        public required ApplicationFilter Filter { get; init; }
        public required IReadOnlyList<JobApplication> Items { get; init; }
        public int Total { get; init; }
        public int Page { get; init; }
        public int PageCount { get; init; }
        public required IReadOnlyList<(string Value, string Label)> Roles { get; init; }
        public required IReadOnlyDictionary<ApplicationStatus, int> Counts { get; init; }

        /// <summary>The selected role's extra questions (filters and answer columns); empty when no role is selected.</summary>
        public IReadOnlyList<CareerRoleQuestion> Questions { get; init; } = [];
    }

    public sealed class ApplicationDetailsModel
    {
        public required JobApplication Application { get; init; }
        public required IReadOnlyDictionary<long, string> Authors { get; init; }
        public long? CurrentUserId { get; init; }

        /// <summary>The role's page address, while the role still exists.</summary>
        public string? RoleSlug { get; init; }
    }

    public static class ApplicationFormat
    {
        /// <summary>An answer as the admin reads it ("yes" / "no" in Arabic, other answers as written).</summary>
        public static string Answer(QuestionKind kind, string value) => kind == QuestionKind.YesNo
            ? value == JobApplicationAnswer.Yes ? "نعم" : value == JobApplicationAnswer.No ? "لا" : value
            : value;

        /// <summary>The answers to one question, joined (a multiple choice has several rows).</summary>
        public static string Answers(IEnumerable<JobApplicationAnswer> rows) => string.Join("، ", rows.Select(r => Answer(r.Kind, r.Value)));

        public static string Status(ApplicationStatus status) => status switch
        {
            ApplicationStatus.New => "جديد",
            ApplicationStatus.Reviewing => "قيد المراجعة",
            ApplicationStatus.Shortlisted => "في القائمة المختصرة",
            ApplicationStatus.Interview => "مقابلة",
            ApplicationStatus.Rejected => "مرفوض",
            ApplicationStatus.Hired => "تم التعيين",
            _ => status.ToString()
        };

        public static string StatusClass(ApplicationStatus status) => status switch
        {
            ApplicationStatus.New => "new",
            ApplicationStatus.Rejected => "off",
            _ => "on"
        };

        public static string Size(long bytes) => bytes >= 1024 * 1024 ? $"{bytes / (1024d * 1024):0.#} MB" : $"{Math.Max(1, bytes / 1024)} KB";
    }
}
