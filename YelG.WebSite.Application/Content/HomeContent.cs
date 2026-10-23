using YelG.WebSite.Domain.Sections;
using YelG.WebSite.Domain.Settings;

namespace YelG.WebSite.Application.Content
{
    /// <summary>
    /// Everything the public homepage shows, in display order. A section is null when it is hidden (or not
    /// seeded); lists hold only published items unless the page is a preview. Read-only: the instances are
    /// shared by every request while cached.
    /// </summary>
    public sealed class HomeContent
    {
        public required SiteSettings Site { get; init; }

        public HeroSection? Hero { get; init; }
        public IReadOnlyList<HeroStat> HeroStats { get; init; } = [];
        public MarqueeSettings? Marquee { get; init; }
        public IReadOnlyList<MarqueeItem> MarqueeItems { get; init; } = [];

        public AboutSection? About { get; init; }
        public IReadOnlyList<AboutValue> AboutValues { get; init; } = [];
        public IReadOnlyList<Credential> Credentials { get; init; } = [];
        public FounderSection? Founder { get; init; }

        public ServicesSection? Services { get; init; }
        public IReadOnlyList<ServiceCategory> ServiceCategories { get; init; } = [];
        /// <summary>Each with its <see cref="Service.Category"/> and ordered deliverables.</summary>
        public IReadOnlyList<Service> ServiceList { get; init; } = [];

        public WhySection? Why { get; init; }
        public IReadOnlyList<WhyCard> WhyCards { get; init; } = [];

        public HowSection? How { get; init; }
        public IReadOnlyList<HowStep> HowSteps { get; init; } = [];

        public PurposeSection? Purpose { get; init; }
        public IReadOnlyList<PurposeGoal> PurposeGoals { get; init; } = [];

        public EngageSection? Engage { get; init; }
        /// <summary>Each with its ordered bullets and pre-selected service.</summary>
        public IReadOnlyList<EngagementModel> EngagementModels { get; init; } = [];

        public IndustriesSection? Industries { get; init; }
        public IReadOnlyList<IndustrySector> Sectors { get; init; } = [];
        public IReadOnlyList<ClientCategory> ClientCategories { get; init; } = [];
        /// <summary>Only logos approved for the web.</summary>
        public IReadOnlyList<ClientLogo> ClientLogos { get; init; } = [];

        public SelfCheckSection? SelfCheck { get; init; }
        public IReadOnlyList<SelfCheckQuestion> SelfCheckQuestions { get; init; } = [];
        public IReadOnlyList<SelfCheckOption> SelfCheckOptions { get; init; } = [];
        public IReadOnlyList<SelfCheckBand> SelfCheckBands { get; init; } = [];

        public FaqSection? Faq { get; init; }
        public IReadOnlyList<FaqItem> FaqItems { get; init; } = [];

        public ConsultSection? Consult { get; init; }
        public IReadOnlyList<ConsultStep> ConsultSteps { get; init; } = [];

        public InsightsSection? Insights { get; init; }
        public IReadOnlyList<InsightPost> InsightPosts { get; init; } = [];

        public CareersSection? Careers { get; init; }
        /// <summary>Active roles with their tags.</summary>
        public IReadOnlyList<CareerRole> CareerRoles { get; init; } = [];

        public ClientPortalSection? Portal { get; init; }

        /// <summary>The self-check renders only when it can produce a result.</summary>
        public bool HasSelfCheck => SelfCheck is not null && SelfCheckQuestions.Count > 0 && SelfCheckOptions.Count > 0 && SelfCheckBands.Count > 0;

        /// <summary>Whether the section an in-page anchor ("#about") points to is on the page; menus hide the others.</summary>
        public bool HasAnchor(string href) => href switch
        {
            "#about" => About is not null,
            "#services" => Services is not null && ServiceList.Count > 0,
            "#why" => Why is not null,
            "#how" => How is not null,
            "#engage" => Engage is not null,
            "#industries" => Industries is not null,
            "#health" => HasSelfCheck,
            "#faq" => Faq is not null && FaqItems.Count > 0,
            "#consult" => Consult is not null,
            "#insights" => Insights is not null,
            "#careers" => Careers is not null,
            "#portal" => Portal is not null,
            _ => true
        };
    }
}
