using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Sections;

namespace YelG.WebSite.Domain.Enquiries
{
    public enum EnquiryStatus
    {
        New = 0,
        Contacted = 1,
        Closed = 2
    }

    /// <summary>A consultation request from the public form. Personal data: deleted for real, by an Admin only.</summary>
    public class Enquiry : BaseEntity
    {
        /// <summary>"ar" or "en": the page language the visitor used.</summary>
        public string Culture { get; set; } = "ar";

        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string? Phone { get; set; }

        /// <summary>The chosen service; null when the visitor chose "not sure yet".</summary>
        public long? ServiceId { get; set; }
        public Service? Service { get; set; }

        /// <summary>The service name as the visitor saw it, kept even if the service is renamed later.</summary>
        public string ServiceLabel { get; set; } = string.Empty;

        public bool IsNotSure { get; set; }

        public string Message { get; set; } = string.Empty;

        /// <summary>When the visitor ticked the consent box (UTC), and the exact wording they agreed to.</summary>
        public DateTime ConsentAt { get; set; }
        public string ConsentText { get; set; } = string.Empty;

        /// <summary>Self-check score included by "discuss your result", when it was used.</summary>
        public int? SelfCheckScore { get; set; }

        public EnquiryStatus Status { get; set; } = EnquiryStatus.New;

        /// <summary>Notification email: when it was sent, or the last error.</summary>
        public DateTime? NotificationSentAt { get; set; }
        public int NotificationAttempts { get; set; }
        public string? NotificationError { get; set; }

        /// <summary>SHA-256 of the client IP with a server-side salt: lets abuse be traced without storing the IP.</summary>
        public string? IpHash { get; set; }

        public string? UserAgent { get; set; }

        public ICollection<EnquiryNote> Notes { get; set; } = new List<EnquiryNote>();
    }

    /// <summary>An internal note on an enquiry (who wrote it is AddedBy_PersonID).</summary>
    public class EnquiryNote : BaseEntity
    {
        public long EnquiryId { get; set; }
        public Enquiry Enquiry { get; set; } = null!;

        public string Text { get; set; } = string.Empty;
    }
}
