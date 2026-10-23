using System.Text.Json;
using System.Text.Json.Serialization;
using YelG.WebSite.Domain.Common;

namespace YelG.WebSite.Infrastructure.Seeding
{
    /// <summary>
    /// Shape of Seeding/Data/seed-content.json, generated from the approved prototypes by
    /// tools/website/extract-seed.py. Texts are {"ar", "en"} pairs; images are file names under /assets.
    /// </summary>
    public sealed class SeedContent
    {
        public static SeedContent Load()
        {
            using var stream = typeof(SeedContent).Assembly.GetManifestResourceStream("YelG.WebSite.Infrastructure.Seeding.Data.seed-content.json")
                ?? throw new InvalidOperationException("seed-content.json is not embedded.");
            return JsonSerializer.Deserialize<SeedContent>(stream, Options)
                ?? throw new InvalidOperationException("seed-content.json is empty.");
        }

        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public SiteSeed Site { get; set; } = new();
        public HeroSeed Hero { get; set; } = new();
        public List<HeroStatSeed> HeroStats { get; set; } = new();
        public MarqueeSeed Marquee { get; set; } = new();
        public AboutSeed About { get; set; } = new();
        public List<LocalizedText> AboutValues { get; set; } = new();
        public List<CredentialSeed> Credentials { get; set; } = new();
        public FounderSeed Founder { get; set; } = new();
        public ServicesSectionSeed ServicesSection { get; set; } = new();
        public List<CategorySeed> ServiceCategories { get; set; } = new();
        public List<ServiceSeed> Services { get; set; } = new();
        public WhySeed Why { get; set; } = new();
        public List<WhyCardSeed> WhyCards { get; set; } = new();
        public SectionSeed How { get; set; } = new();
        public List<WhyCardSeed> HowSteps { get; set; } = new();
        public PurposeSeed Purpose { get; set; } = new();
        public List<LocalizedText> PurposeGoals { get; set; } = new();
        public SectionSeed Engage { get; set; } = new();
        public List<EngagementSeed> EngagementModels { get; set; } = new();
        public IndustriesSeed Industries { get; set; } = new();
        public List<SectorSeed> Sectors { get; set; } = new();
        public List<ClientCategorySeed> ClientCategories { get; set; } = new();
        public SelfCheckSeed SelfCheck { get; set; } = new();
        public List<QuestionSeed> SelfCheckQuestions { get; set; } = new();
        public List<OptionSeed> SelfCheckOptions { get; set; } = new();
        public List<BandSeed> SelfCheckBands { get; set; } = new();
        public SectionSeed Faq { get; set; } = new();
        public List<FaqSeed> FaqItems { get; set; } = new();
        public ConsultSeed Consult { get; set; } = new();
        public List<WhyCardSeed> ConsultSteps { get; set; } = new();
        public InsightsSeed Insights { get; set; } = new();
        public List<PostSeed> InsightPosts { get; set; } = new();
        public CareersSeed Careers { get; set; } = new();
        public PortalSeed Portal { get; set; } = new();
        public List<string> Media { get; set; } = new();

        /// <summary>Client-side validation messages of the design, per language (also used by the server).</summary>
        public Dictionary<string, Dictionary<string, string>> Validation { get; set; } = new();

        /// <summary>The service-list value that means "not sure yet", per language.</summary>
        public LocalizedText NotSureValue { get; set; } = new();
    }

    public class SectionSeed
    {
        public LocalizedText Eyebrow { get; set; } = new();
        public LocalizedText Heading { get; set; } = new();
        public bool HideHeadingBreakOnMobile { get; set; }
        public LocalizedText Lead { get; set; } = new();
    }

    public sealed class SiteSeed
    {
        public LocalizedText Tagline { get; set; } = new();
        public string ContactEmail { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string WebsiteUrl { get; set; } = string.Empty;
        public string LinkedInUrl { get; set; } = string.Empty;
        public string CareersEmail { get; set; } = string.Empty;
        public LocalizedText SeoTitle { get; set; } = new();
        public LocalizedText SeoDescription { get; set; } = new();
        public LocalizedText Copyright { get; set; } = new();
    }

    public sealed class HeroSeed : SectionSeed
    {
        public LocalizedText PrimaryCtaLabel { get; set; } = new();
        public string PrimaryCtaTarget { get; set; } = string.Empty;
        public LocalizedText SecondaryCtaLabel { get; set; } = new();
        public string SecondaryCtaTarget { get; set; } = string.Empty;
        public LocalizedText SelfCheckLinkText { get; set; } = new();
        public string Image { get; set; } = string.Empty;
        public LocalizedText ImageAlt { get; set; } = new();
        public string BadgeRingText { get; set; } = string.Empty;
    }

    public sealed class HeroStatSeed
    {
        public Domain.Sections.HeroStatKind Kind { get; set; }
        public LocalizedText Value { get; set; } = new();
        public LocalizedText Caption { get; set; } = new();
        public LocalizedText ScreenReaderLabel { get; set; } = new();
        public string ChipCodes { get; set; } = string.Empty;
    }

    public sealed class MarqueeSeed
    {
        public LocalizedText ScreenReaderText { get; set; } = new();
        public List<LocalizedText> Items { get; set; } = new();
    }

    public sealed class AboutSeed : SectionSeed
    {
        public LocalizedText CredentialsLabel { get; set; } = new();
        public string Image1 { get; set; } = string.Empty;
        public LocalizedText Image1Alt { get; set; } = new();
        public string Image2 { get; set; } = string.Empty;
        public LocalizedText Image2Alt { get; set; } = new();
        public string BadgeRingText { get; set; } = string.Empty;
    }

    public sealed class CredentialSeed
    {
        public string Code { get; set; } = string.Empty;
        public LocalizedText Description { get; set; } = new();
    }

    public sealed class FounderSeed
    {
        public LocalizedText Eyebrow { get; set; } = new();
        public LocalizedText Quote { get; set; } = new();
        public LocalizedText SupportingLine { get; set; } = new();
        public LocalizedText Name { get; set; } = new();
        public LocalizedText Title { get; set; } = new();
        public string Photo { get; set; } = string.Empty;
        public LocalizedText PhotoAlt { get; set; } = new();
        public string Avatar { get; set; } = string.Empty;
    }

    public sealed class ServicesSectionSeed : SectionSeed
    {
        public LocalizedText AllTabLabel { get; set; } = new();
        public LocalizedText CardLinkLabel { get; set; } = new();
        public LocalizedText ScopeHeading { get; set; } = new();
        public LocalizedText DiscussCtaLabel { get; set; } = new();
    }

    public sealed class CategorySeed
    {
        public string Key { get; set; } = string.Empty;
        public LocalizedText Name { get; set; } = new();
    }

    public sealed class ServiceSeed
    {
        public string Key { get; set; } = string.Empty;
        public string IconKey { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public LocalizedText Name { get; set; } = new();
        public LocalizedText ShortDescription { get; set; } = new();
        public LocalizedText LongDescription { get; set; } = new();
        public List<LocalizedText> Deliverables { get; set; } = new();
    }

    public sealed class WhySeed : SectionSeed
    {
        public string Image { get; set; } = string.Empty;
        public LocalizedText ImageAlt { get; set; } = new();
        public LocalizedText FloatingCardText { get; set; } = new();
    }

    /// <summary>Title + text items (why cards, how steps, consultation steps).</summary>
    public sealed class WhyCardSeed
    {
        public LocalizedText Title { get; set; } = new();
        public LocalizedText Text { get; set; } = new();
        public bool IsHighlighted { get; set; }
    }

    public sealed class PurposeSeed : SectionSeed
    {
        public LocalizedText MissionTabLabel { get; set; } = new();
        public LocalizedText VisionTabLabel { get; set; } = new();
        public LocalizedText GoalsTabLabel { get; set; } = new();
        public LocalizedText Mission { get; set; } = new();
        public LocalizedText Vision { get; set; } = new();
        public string Video { get; set; } = string.Empty;
        public string Poster { get; set; } = string.Empty;
        public LocalizedText VideoLabel { get; set; } = new();
    }

    public sealed class EngagementSeed
    {
        public string IconKey { get; set; } = string.Empty;
        public LocalizedText Title { get; set; } = new();
        public LocalizedText Text { get; set; } = new();
        public List<LocalizedText> Bullets { get; set; } = new();
        public LocalizedText CtaLabel { get; set; } = new();
        public string? PreselectService { get; set; }
        public bool PreselectNotSure { get; set; }
        public bool IsHighlighted { get; set; }
    }

    public sealed class IndustriesSeed : SectionSeed
    {
        public LocalizedText ApprovalNote { get; set; } = new();
    }

    public sealed class SectorSeed
    {
        public LocalizedText Label { get; set; } = new();
        public bool IsEmphasized { get; set; }
    }

    public sealed class ClientCategorySeed
    {
        public string IconKey { get; set; } = string.Empty;
        public string ColorVariant { get; set; } = string.Empty;
        public LocalizedText Title { get; set; } = new();
        public LocalizedText Text { get; set; } = new();
    }

    public sealed class SelfCheckSeed : SectionSeed
    {
        public LocalizedText Disclaimer1 { get; set; } = new();
        public LocalizedText Disclaimer2 { get; set; } = new();
        public LocalizedText PickHint { get; set; } = new();
        public LocalizedText BackLabel { get; set; } = new();
        public LocalizedText ResultLabel { get; set; } = new();
        public LocalizedText RecommendationsLabel { get; set; } = new();
        public LocalizedText MethodNote { get; set; } = new();
        public LocalizedText DiscussCtaLabel { get; set; } = new();
        public LocalizedText RestartLabel { get; set; } = new();
        public LocalizedText PrefillTemplate { get; set; } = new();
    }

    public sealed class QuestionSeed
    {
        public LocalizedText Area { get; set; } = new();
        public LocalizedText Text { get; set; } = new();
        public string LinkedService { get; set; } = string.Empty;
    }

    public sealed class OptionSeed
    {
        public LocalizedText Label { get; set; } = new();
        public int Score { get; set; }
    }

    public sealed class BandSeed
    {
        public int MinScore { get; set; }
        public LocalizedText Title { get; set; } = new();
        public LocalizedText Text { get; set; } = new();
    }

    public sealed class FaqSeed
    {
        public LocalizedText Question { get; set; } = new();
        public LocalizedText Answer { get; set; } = new();
        public bool IsOpenByDefault { get; set; }
    }

    public sealed class ConsultSeed : SectionSeed
    {
        public LocalizedText DirectContactLabel { get; set; } = new();
        public LocalizedText FormTitle { get; set; } = new();
        public LocalizedText RequiredNote { get; set; } = new();
        public LocalizedText ConsentText { get; set; } = new();
        public LocalizedText NotSureLabel { get; set; } = new();
        public LocalizedText SubmitLabel { get; set; } = new();
        public LocalizedText SuccessTitle { get; set; } = new();
        public LocalizedText SuccessText { get; set; } = new();
        public LocalizedText FailureText { get; set; } = new();
    }

    public sealed class InsightsSeed : SectionSeed
    {
        public LocalizedText FollowLinkLabel { get; set; } = new();
        public string FollowLinkUrl { get; set; } = string.Empty;
    }

    public sealed class PostSeed
    {
        public string Image { get; set; } = string.Empty;
        public LocalizedText ImageAlt { get; set; } = new();
        public LocalizedText CategoryLabel { get; set; } = new();
        public string CategoryColor { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateOnly PublishedOn { get; set; }
        public LocalizedText Title { get; set; } = new();
        public string ExternalUrl { get; set; } = string.Empty;
    }

    public sealed class CareersSeed : SectionSeed
    {
        public string Image { get; set; } = string.Empty;
        public LocalizedText ImageAlt { get; set; } = new();
        public LocalizedText SendCvLabel { get; set; } = new();
        public string CvEmailSubject { get; set; } = string.Empty;
        public LocalizedText JobsLinkLabel { get; set; } = new();
        public string JobsUrl { get; set; } = string.Empty;
    }

    public sealed class PortalSeed
    {
        public LocalizedText BadgeLabel { get; set; } = new();
        public LocalizedText Title { get; set; } = new();
        public LocalizedText Text { get; set; } = new();
        public Domain.Sections.PortalStatus Status { get; set; }
        public LocalizedText SignInLabel { get; set; } = new();
        public string? PortalUrl { get; set; }
    }
}
