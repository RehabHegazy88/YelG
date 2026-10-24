using System.Globalization;
using YelG.WebSite.Domain.Enquiries;

namespace YelG.WebSite.Areas.Admin.Models
{
    /// <summary>The inbox filters, from the query string.</summary>
    public sealed record EnquiryFilter
    {
        public EnquiryStatus? Status { get; init; }

        /// <summary>A service id, or "not-sure".</summary>
        public string? Service { get; init; }

        /// <summary>Searches the name, email and company.</summary>
        public string? Q { get; init; }

        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }

        /// <summary>The filters as a query string (for paging and the export link).</summary>
        public string Query(int? page = null) => string.Join("&", new[]
        {
            Status is null ? null : $"status={Status}",
            string.IsNullOrEmpty(Service) ? null : "service=" + Uri.EscapeDataString(Service),
            string.IsNullOrWhiteSpace(Q) ? null : "q=" + Uri.EscapeDataString(Q),
            From is null ? null : "from=" + From.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            To is null ? null : "to=" + To.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            page is > 1 ? $"page={page}" : null
        }.Where(x => x is not null));
    }

    public sealed class EnquiryListModel
    {
        public required EnquiryFilter Filter { get; init; }
        public required IReadOnlyList<Enquiry> Items { get; init; }
        public int Total { get; init; }
        public int Page { get; init; }
        public int PageCount { get; init; }
        public required IReadOnlyList<(string Value, string Label)> Services { get; init; }
        public required IReadOnlyDictionary<EnquiryStatus, int> Counts { get; init; }
    }

    public sealed class EnquiryDetailsModel
    {
        public required Enquiry Enquiry { get; init; }
        public required IReadOnlyDictionary<long, string> Authors { get; init; }
        public long? CurrentUserId { get; init; }
    }

    public static class EnquiryFormat
    {
        public static string Status(EnquiryStatus status) => status switch
        {
            EnquiryStatus.New => "جديد",
            EnquiryStatus.Contacted => "تم التواصل",
            EnquiryStatus.Closed => "مغلق",
            _ => status.ToString()
        };

        public static string StatusClass(EnquiryStatus status) => status switch
        {
            EnquiryStatus.New => "new",
            EnquiryStatus.Contacted => "on",
            _ => "off"
        };
    }
}
