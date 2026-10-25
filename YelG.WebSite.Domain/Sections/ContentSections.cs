using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Media;

namespace YelG.WebSite.Domain.Sections
{
    // ---------------- Why YelG ----------------

    public class WhySection : SectionBase
    {
        public long? ImageId { get; set; }
        public MediaAsset? Image { get; set; }
        public LocalizedText ImageAlt { get; set; } = new();

        /// <summary>White card floating over the photo ("من الأرقام إلى قرارات أوضح").</summary>
        public LocalizedText FloatingCardText { get; set; } = new();
    }

    /// <summary>A differentiator card; a highlighted one renders navy.</summary>
    public class WhyCard : ContentItem
    {
        public LocalizedText Title { get; set; } = new();
        public LocalizedText Text { get; set; } = new();
        public bool IsHighlighted { get; set; }
    }

    // ---------------- How we work ----------------

    public class HowSection : SectionBase
    {
    }

    /// <summary>A numbered step; a highlighted one has the cyan dot (the last step in the design).</summary>
    public class HowStep : ContentItem
    {
        public LocalizedText Title { get; set; } = new();
        public LocalizedText Text { get; set; } = new();
        public bool IsHighlighted { get; set; }
    }

    // ---------------- Purpose (mission, vision, goals, brand film) ----------------

    public class PurposeSection : SectionBase
    {
        public LocalizedText MissionTabLabel { get; set; } = new();
        public LocalizedText Mission { get; set; } = new();

        public LocalizedText VisionTabLabel { get; set; } = new();
        public LocalizedText Vision { get; set; } = new();

        public LocalizedText GoalsTabLabel { get; set; } = new();

        public long? VideoId { get; set; }
        public MediaAsset? Video { get; set; }

        public long? PosterId { get; set; }
        public MediaAsset? Poster { get; set; }

        public LocalizedText VideoLabel { get; set; } = new();
    }

    public class PurposeGoal : ContentItem
    {
        public LocalizedText Label { get; set; } = new();
    }

    // ---------------- Ways to work with us ----------------

    public class EngageSection : SectionBase
    {
    }

    /// <summary>
    /// An engagement model card. Its button pre-fills the consultation form with a service, or with
    /// "not sure yet" when <see cref="PreselectNotSure"/> is set.
    /// </summary>
    public class EngagementModel : ContentItem
    {
        /// <summary>One of <see cref="IconKeys.Service"/>.</summary>
        public string IconKey { get; set; } = string.Empty;

        public LocalizedText Title { get; set; } = new();
        public LocalizedText Text { get; set; } = new();
        public LocalizedText CtaLabel { get; set; } = new();

        public long? PreselectServiceId { get; set; }
        public Service? PreselectService { get; set; }
        public bool PreselectNotSure { get; set; }

        /// <summary>Navy card with the cyan button (the middle card in the design).</summary>
        public bool IsHighlighted { get; set; }

        public ICollection<EngagementBullet> Bullets { get; set; } = new List<EngagementBullet>();
    }

    public class EngagementBullet : ContentItem
    {
        public long EngagementModelId { get; set; }
        public EngagementModel EngagementModel { get; set; } = null!;

        public LocalizedText Text { get; set; } = new();
    }

    // ---------------- Industries & clients ----------------

    public class IndustriesSection : SectionBase
    {
        /// <summary>Line under the cards ("لا ننشر أسماء العملاء أو شعاراتهم إلا بموافقة كتابية مسبقة منهم.").</summary>
        public LocalizedText ApprovalNote { get; set; } = new();
    }

    /// <summary>A sector chip; an emphasised one renders navy ("وقطاعات أخرى").</summary>
    public class IndustrySector : ContentItem
    {
        public LocalizedText Label { get; set; } = new();
        public bool IsEmphasized { get; set; }
    }

    public class ClientCategory : ContentItem
    {
        /// <summary>One of <see cref="IconKeys.ClientCategory"/>.</summary>
        public string IconKey { get; set; } = string.Empty;

        /// <summary>One of <see cref="IconKeys.ClientCategoryColor"/>.</summary>
        public string ColorVariant { get; set; } = "blue";

        public LocalizedText Title { get; set; } = new();
        public LocalizedText Text { get; set; } = new();
    }

    /// <summary>A client logo. Only logos with written approval (<see cref="IsApprovedForWeb"/>) render.</summary>
    public class ClientLogo : ContentItem
    {
        public LocalizedText Name { get; set; } = new();

        public long? LogoId { get; set; }
        public MediaAsset? Logo { get; set; }

        public string? WebsiteUrl { get; set; }

        public bool IsApprovedForWeb { get; set; }
    }

    // ---------------- FAQ ----------------

    public class FaqSection : SectionBase
    {
    }

    public class FaqItem : ContentItem
    {
        public LocalizedText Question { get; set; } = new();

        /// <summary>May contain a [link](#consult).</summary>
        public LocalizedText Answer { get; set; } = new();

        public bool IsOpenByDefault { get; set; }
    }

    // ---------------- Consultation (left panel and form texts) ----------------

    public class ConsultSection : SectionBase
    {
        /// <summary>Label above the direct email and phone ("تفضّل التواصل المباشر؟").</summary>
        public LocalizedText DirectContactLabel { get; set; } = new();

        public LocalizedText FormTitle { get; set; } = new();

        /// <summary>"الحقول المميزة بعلامة * إلزامية."</summary>
        public LocalizedText RequiredNote { get; set; } = new();

        public LocalizedText ConsentText { get; set; } = new();

        /// <summary>The "not sure yet" choice of the service list (its label; the stored value is "not sure").</summary>
        public LocalizedText NotSureLabel { get; set; } = new();

        public LocalizedText SubmitLabel { get; set; } = new();

        /// <summary>Shown only after the enquiry was saved.</summary>
        public LocalizedText SuccessTitle { get; set; } = new();
        public LocalizedText SuccessText { get; set; } = new();

        /// <summary>Shown when the enquiry could not be saved.</summary>
        public LocalizedText FailureText { get; set; } = new();
    }

    /// <summary>A numbered step in the consultation panel ("شاركنا احتياجاتك").</summary>
    public class ConsultStep : ContentItem
    {
        public LocalizedText Title { get; set; } = new();
        public LocalizedText Text { get; set; } = new();
    }

    // ---------------- Insights ----------------

    public class InsightsSection : SectionBase
    {
        public LocalizedText FollowLinkLabel { get; set; } = new();
        public string FollowLinkUrl { get; set; } = string.Empty;
    }

    public class InsightPost : ContentItem
    {
        public long? ImageId { get; set; }
        public MediaAsset? Image { get; set; }
        public LocalizedText ImageAlt { get; set; } = new();

        /// <summary>"رؤية", "سلسلة", …</summary>
        public LocalizedText CategoryLabel { get; set; } = new();

        /// <summary>One of <see cref="IconKeys.InsightColor"/>.</summary>
        public string CategoryColor { get; set; } = "blue";

        /// <summary>Where it was published ("LinkedIn").</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>Shown as month and year.</summary>
        public DateOnly PublishedOn { get; set; }

        public LocalizedText Title { get; set; } = new();

        public string ExternalUrl { get; set; } = string.Empty;
    }

    // ---------------- Careers ----------------

    public class CareersSection : SectionBase
    {
        public long? ImageId { get; set; }
        public MediaAsset? Image { get; set; }
        public LocalizedText ImageAlt { get; set; } = new();

        public LocalizedText SendCvLabel { get; set; } = new();

        /// <summary>Subject of the "send your CV" email.</summary>
        public string CvEmailSubject { get; set; } = string.Empty;

        public LocalizedText JobsLinkLabel { get; set; } = new();
        public string JobsUrl { get; set; } = string.Empty;

        // ---- the careers page (/{lang}/careers) and its application form; empty texts use the site's defaults

        /// <summary>The careers page's own heading and lead (the homepage card keeps <see cref="SectionBase.Heading"/>).</summary>
        public LocalizedText PageHeading { get; set; } = new();
        public LocalizedText PageLead { get; set; } = new();

        /// <summary>Heading of the application form on a job's page ("قدّم على هذه الوظيفة").</summary>
        public LocalizedText ApplyTitle { get; set; } = new();

        /// <summary>What the applicant agrees to (use of the CV, how long it is kept).</summary>
        public LocalizedText ApplyConsentText { get; set; } = new();

        /// <summary>Shown once the application is saved.</summary>
        public LocalizedText ApplySuccessTitle { get; set; } = new();
        public LocalizedText ApplySuccessText { get; set; } = new();

        /// <summary>Shown on the careers page when no role is open.</summary>
        public LocalizedText NoOpeningsText { get; set; } = new();
    }

    public enum EmploymentType
    {
        FullTime = 0,
        PartTime = 1,
        Contract = 2,
        Internship = 3
    }

    /// <summary>
    /// An open role, with its own page (/{lang}/careers/{slug}) and application form. None in the approved
    /// design; the lists show only roles that are active and not past their closing date.
    /// </summary>
    public class CareerRole : ContentItem
    {
        /// <summary>The page address: lower-case Latin letters, digits and hyphens ("senior-accountant").</summary>
        public string Slug { get; set; } = string.Empty;

        public LocalizedText Title { get; set; } = new();
        public LocalizedText Department { get; set; } = new();
        public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
        public LocalizedText Location { get; set; } = new();
        public LocalizedText Experience { get; set; } = new();

        /// <summary>One or two sentences for the list.</summary>
        public LocalizedText Summary { get; set; } = new();

        /// <summary>About the role (paragraphs).</summary>
        public LocalizedText Description { get; set; } = new();

        /// <summary>One item per line.</summary>
        public LocalizedText Responsibilities { get; set; } = new();

        /// <summary>One item per line.</summary>
        public LocalizedText Requirements { get; set; } = new();

        public DateOnly? PostedOn { get; set; }

        /// <summary>The last day to apply; after it the role is no longer listed.</summary>
        public DateOnly? ClosingOn { get; set; }

        /// <summary>
        /// Optional: an external application page (https) or address. Without them, candidates apply with the
        /// form on the role's page.
        /// </summary>
        public string? ApplyUrl { get; set; }
        public string? ApplyEmail { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Listed on the site on <paramref name="today"/>: active and not past its closing date.</summary>
        public bool IsOpenOn(DateOnly today) => IsActive && (ClosingOn is null || ClosingOn >= today);

        public ICollection<CareerRoleTag> Tags { get; set; } = new List<CareerRoleTag>();
    }

    public class CareerRoleTag : ContentItem
    {
        public long CareerRoleId { get; set; }
        public CareerRole CareerRole { get; set; } = null!;

        public LocalizedText Label { get; set; } = new();
    }

    // ---------------- Client portal ----------------

    public enum PortalStatus
    {
        ComingSoon = 0,
        Live = 1
    }

    /// <summary>The client portal card. The sign-in button renders only when Live and a URL is set.</summary>
    public class ClientPortalSection : BaseEntity
    {
        public bool IsPublished { get; set; } = true;

        public PortalStatus Status { get; set; } = PortalStatus.ComingSoon;

        /// <summary>Badge shown while coming soon ("قريبًا").</summary>
        public LocalizedText BadgeLabel { get; set; } = new();

        public LocalizedText Title { get; set; } = new();
        public LocalizedText Text { get; set; } = new();
        public LocalizedText SignInLabel { get; set; } = new();
        public string? PortalUrl { get; set; }

        public bool ShowsSignIn => Status == PortalStatus.Live && !string.IsNullOrWhiteSpace(PortalUrl);
    }
}
