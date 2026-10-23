using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Media;

namespace YelG.WebSite.Domain.Sections
{
    /// <summary>
    /// The hero. Heading is the h1 (its *highlight* renders in cyan); Lead is the supporting text.
    /// The header's "book a consultation" button reuses the primary call to action.
    /// </summary>
    public class HeroSection : SectionBase
    {
        public LocalizedText PrimaryCtaLabel { get; set; } = new();
        public string PrimaryCtaTarget { get; set; } = "#consult";

        public LocalizedText SecondaryCtaLabel { get; set; } = new();
        public string SecondaryCtaTarget { get; set; } = "#services";

        /// <summary>Underlined link to the self-check under the buttons.</summary>
        public LocalizedText SelfCheckLinkText { get; set; } = new();

        public long? ImageId { get; set; }
        public MediaAsset? Image { get; set; }
        public LocalizedText ImageAlt { get; set; } = new();

        /// <summary>Text on the rotating badge (Latin in both languages), e.g. "INFINITE FINANCIAL CARE • SINCE 2017 •".</summary>
        public string BadgeRingText { get; set; } = string.Empty;
    }

    public enum HeroStatKind
    {
        /// <summary>A big number with a caption ("2017").</summary>
        Number = 0,

        /// <summary>A caption followed by small chips ("CMA · CIC · CWM").</summary>
        Chips = 1
    }

    /// <summary>One of the hero stats (the design shows three).</summary>
    public class HeroStat : ContentItem
    {
        public HeroStatKind Kind { get; set; }

        /// <summary>For Number: the figure, shown left-to-right ("2017", "+10").</summary>
        public string Value { get; set; } = string.Empty;

        public LocalizedText Caption { get; set; } = new();

        /// <summary>Screen-reader term for the stat (the dt), e.g. "سنة التأسيس".</summary>
        public LocalizedText ScreenReaderLabel { get; set; } = new();

        /// <summary>For Chips: the chip texts, comma-separated ("CMA,CIC,CWM").</summary>
        public string ChipCodes { get; set; } = string.Empty;
    }

    /// <summary>The cyan services strip under the hero.</summary>
    public class MarqueeSettings : BaseEntity
    {
        public bool IsPublished { get; set; } = true;

        /// <summary>Full sentence read to screen readers instead of the moving strip.</summary>
        public LocalizedText ScreenReaderText { get; set; } = new();
    }

    /// <summary>A label in the moving strip. Its own text: shorter than the service names in places.</summary>
    public class MarqueeItem : ContentItem
    {
        public LocalizedText Label { get; set; } = new();
    }
}
