using YelG.WebSite.Domain.Common;

namespace YelG.WebSite.Domain.Sections
{
    /// <summary>Texts of the services section around the categories and cards.</summary>
    public class ServicesSection : SectionBase
    {
        /// <summary>The first tab ("جميع الخدمات").</summary>
        public LocalizedText AllTabLabel { get; set; } = new();

        /// <summary>Link on each card ("تفاصيل الخدمة").</summary>
        public LocalizedText CardLinkLabel { get; set; } = new();

        /// <summary>Heading of the deliverables list in the detail panel ("نطاق الخدمة").</summary>
        public LocalizedText ScopeHeading { get; set; } = new();

        /// <summary>Button in the detail panel that pre-fills the form ("ناقش هذه الخدمة").</summary>
        public LocalizedText DiscussCtaLabel { get; set; } = new();
    }

    public class ServiceCategory : ContentItem
    {
        /// <summary>Stable key: "ops", "adv", "dig", "risk".</summary>
        public string Key { get; set; } = string.Empty;

        public LocalizedText Name { get; set; } = new();

        public ICollection<Service> Services { get; set; } = new List<Service>();
    }

    public class Service : ContentItem
    {
        /// <summary>Stable key: "acc", "treas", … (used by the form, the self-check and links).</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>One of <see cref="IconKeys.Service"/>.</summary>
        public string IconKey { get; set; } = string.Empty;

        public long CategoryId { get; set; }
        public ServiceCategory Category { get; set; } = null!;

        public LocalizedText Name { get; set; } = new();

        /// <summary>Card text.</summary>
        public LocalizedText ShortDescription { get; set; } = new();

        /// <summary>Detail panel text.</summary>
        public LocalizedText LongDescription { get; set; } = new();

        public ICollection<ServiceDeliverable> Deliverables { get; set; } = new List<ServiceDeliverable>();
    }

    /// <summary>One line of a service's scope ("دليل الحسابات وهيكل التكاليف").</summary>
    public class ServiceDeliverable : ContentItem
    {
        public long ServiceId { get; set; }
        public Service Service { get; set; } = null!;

        public LocalizedText Text { get; set; } = new();
    }
}
