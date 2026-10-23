using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Media;

namespace YelG.WebSite.Domain.Settings
{
    /// <summary>Site-wide settings (one row).</summary>
    public class SiteSettings : BaseEntity
    {
        /// <summary>Optional uploaded logo; without one the built-in SVG mark and "YelG ADVISORY" render, as designed.</summary>
        public long? LogoId { get; set; }
        public MediaAsset? Logo { get; set; }

        /// <summary>Footer line under the logo.</summary>
        public LocalizedText Tagline { get; set; } = new();

        /// <summary>Shown on the site (consultation panel, footer).</summary>
        public string ContactEmail { get; set; } = string.Empty;

        /// <summary>Where new enquiries are emailed; usually the contact email.</summary>
        public string NotificationEmail { get; set; } = string.Empty;

        /// <summary>Display form, e.g. "+20 10 0667 7363". The tel: link is derived from it.</summary>
        public string Phone { get; set; } = string.Empty;

        public string WebsiteUrl { get; set; } = string.Empty;

        public string LinkedInUrl { get; set; } = string.Empty;

        public string CareersEmail { get; set; } = string.Empty;

        /// <summary>"ar" or "en": where "/" sends visitors.</summary>
        public string DefaultLanguage { get; set; } = "ar";

        public LocalizedText SeoTitle { get; set; } = new();

        public LocalizedText SeoDescription { get; set; } = new();

        public long? OgImageId { get; set; }
        public MediaAsset? OgImage { get; set; }

        /// <summary>Footer copyright line; "{year}" is replaced by the current year.</summary>
        public LocalizedText Copyright { get; set; } = new();
    }
}
