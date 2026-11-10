using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Sections;

namespace YelG.WebSite.Domain.Careers
{
    public enum ApplicationStatus
    {
        New = 0,
        Reviewing = 1,
        Shortlisted = 2,
        Interview = 3,
        Rejected = 4,
        Hired = 5
    }

    /// <summary>
    /// An application sent with the form on a role's page. Personal data: deleted for real (with its CV) by an
    /// Admin only. The CV is kept outside the public files and opened from the admin panel only.
    /// </summary>
    public class JobApplication : BaseEntity
    {
        public long? CareerRoleId { get; set; }
        public CareerRole? CareerRole { get; set; }

        /// <summary>The role's title as the applicant saw it, kept if the role is renamed or removed.</summary>
        public string RoleTitle { get; set; } = string.Empty;

        /// <summary>"ar" or "en": the page language the applicant used.</summary>
        public string Culture { get; set; } = "ar";

        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? Message { get; set; }

        /// <summary>The file name as uploaded (shown and used for the download).</summary>
        public string CvFileName { get; set; } = string.Empty;

        /// <summary>Where the CV is kept in the private store (generated name).</summary>
        public string CvStoredPath { get; set; } = string.Empty;

        public string CvContentType { get; set; } = string.Empty;
        public long CvSizeBytes { get; set; }

        /// <summary>When the applicant ticked the consent box (UTC), and the exact wording they agreed to.</summary>
        public DateTime ConsentAt { get; set; }
        public string ConsentText { get; set; } = string.Empty;

        public ApplicationStatus Status { get; set; } = ApplicationStatus.New;

        /// <summary>Notification email to the careers address: when it was sent, or the last error.</summary>
        public DateTime? NotificationSentAt { get; set; }
        public int NotificationAttempts { get; set; }
        public string? NotificationError { get; set; }

        /// <summary>SHA-256 of the client IP with a server-side salt.</summary>
        public string? IpHash { get; set; }

        public string? UserAgent { get; set; }

        public ICollection<JobApplicationNote> Notes { get; set; } = new List<JobApplicationNote>();

        /// <summary>The answers to the role's extra questions (one row per chosen option for a multiple choice).</summary>
        public ICollection<JobApplicationAnswer> Answers { get; set; } = new List<JobApplicationAnswer>();
    }

    /// <summary>
    /// An answer to one of the role's extra questions. The question's wording (Arabic) and kind are copied, so the
    /// answer still reads right if the question is changed or removed. <see cref="Value"/> is "yes" or "no", the
    /// text written, or one chosen option in Arabic.
    /// </summary>
    public class JobApplicationAnswer : BaseEntity
    {
        public const string Yes = "yes";
        public const string No = "no";

        public long JobApplicationId { get; set; }
        public JobApplication JobApplication { get; set; } = null!;

        /// <summary>The question, while it exists.</summary>
        public long? CareerRoleQuestionId { get; set; }

        public string Question { get; set; } = string.Empty;
        public QuestionKind Kind { get; set; }
        public string Value { get; set; } = string.Empty;

        /// <summary>The question's place on the form, to show the answers in the same order.</summary>
        public int SortOrder { get; set; }
    }

    /// <summary>An internal note on an application (who wrote it is AddedBy_PersonID).</summary>
    public class JobApplicationNote : BaseEntity
    {
        public long JobApplicationId { get; set; }
        public JobApplication JobApplication { get; set; } = null!;

        public string Text { get; set; } = string.Empty;
    }
}
