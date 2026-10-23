using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Media;

namespace YelG.WebSite.Domain.Sections
{
    /// <summary>"About us": heading, paragraph (Lead), values, credentials and the two-photo collage.</summary>
    public class AboutSection : SectionBase
    {
        /// <summary>Label above the credentials ("مؤهلات فريق العمل").</summary>
        public LocalizedText CredentialsLabel { get; set; } = new();

        /// <summary>The large photo of the collage.</summary>
        public long? Image1Id { get; set; }
        public MediaAsset? Image1 { get; set; }
        public LocalizedText Image1Alt { get; set; } = new();

        /// <summary>The smaller, framed photo of the collage.</summary>
        public long? Image2Id { get; set; }
        public MediaAsset? Image2 { get; set; }
        public LocalizedText Image2Alt { get; set; } = new();

        public string BadgeRingText { get; set; } = string.Empty;
    }

    /// <summary>A company value with a check mark ("النزاهة").</summary>
    public class AboutValue : ContentItem
    {
        public LocalizedText Label { get; set; } = new();
    }

    /// <summary>A team qualification card ("CMA" + "محاسب إداري معتمد").</summary>
    public class Credential : ContentItem
    {
        public string Code { get; set; } = string.Empty;

        public LocalizedText Description { get; set; } = new();
    }

    /// <summary>The founder's word (part of the About section in the design).</summary>
    public class FounderSection : BaseEntity
    {
        public bool IsPublished { get; set; } = true;

        public LocalizedText Eyebrow { get; set; } = new();

        public LocalizedText Quote { get; set; } = new();

        /// <summary>The paragraph under the quote.</summary>
        public LocalizedText SupportingLine { get; set; } = new();

        public LocalizedText Name { get; set; } = new();

        public LocalizedText Title { get; set; } = new();

        /// <summary>Cut-out portrait shown in the coloured arch.</summary>
        public long? PhotoId { get; set; }
        public MediaAsset? Photo { get; set; }
        public LocalizedText PhotoAlt { get; set; } = new();

        /// <summary>Small round avatar next to the name.</summary>
        public long? AvatarId { get; set; }
        public MediaAsset? Avatar { get; set; }
    }
}
