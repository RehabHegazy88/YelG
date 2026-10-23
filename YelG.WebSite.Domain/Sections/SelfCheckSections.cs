using YelG.WebSite.Domain.Common;

namespace YelG.WebSite.Domain.Sections
{
    /// <summary>
    /// The financial self-check. The score is the sum of the chosen options; its maximum and the question
    /// count shown ("/ 20", "Question 3 of 10") come from the data. Texts that state those numbers
    /// (the lead, <see cref="MethodNote"/>) are copy and are updated by the editor.
    /// </summary>
    public class SelfCheckSection : SectionBase
    {
        /// <summary>First bullet under the lead ("أداة استرشادية، …").</summary>
        public LocalizedText Disclaimer1 { get; set; } = new();

        /// <summary>Second bullet ("تبقى إجاباتك على هذه الصفحة فقط، …").</summary>
        public LocalizedText Disclaimer2 { get; set; } = new();

        public LocalizedText PickHint { get; set; } = new();
        public LocalizedText BackLabel { get; set; } = new();
        public LocalizedText ResultLabel { get; set; } = new();
        public LocalizedText RecommendationsLabel { get; set; } = new();

        /// <summary>How the score is counted, under the result.</summary>
        public LocalizedText MethodNote { get; set; } = new();

        public LocalizedText DiscussCtaLabel { get; set; } = new();
        public LocalizedText RestartLabel { get; set; } = new();

        /// <summary>Message put in the consultation form; placeholders {score}, {max} and {band}.</summary>
        public LocalizedText PrefillTemplate { get; set; } = new();
    }

    public class SelfCheckQuestion : ContentItem
    {
        /// <summary>Area shown next to the progress ("السجلات").</summary>
        public LocalizedText Area { get; set; } = new();

        public LocalizedText Text { get; set; } = new();

        /// <summary>Recommended when this question is not answered with the top option.</summary>
        public long? LinkedServiceId { get; set; }
        public Service? LinkedService { get; set; }
    }

    /// <summary>An answer every question offers ("نعم" = 2, "جزئيًا" = 1, "ليس بعد" = 0).</summary>
    public class SelfCheckOption : ContentItem
    {
        public LocalizedText Label { get; set; } = new();
        public int Score { get; set; }
    }

    /// <summary>A result band: applies from <see cref="MinScore"/> up to the next band.</summary>
    public class SelfCheckBand : ContentItem
    {
        public int MinScore { get; set; }
        public LocalizedText Title { get; set; } = new();
        public LocalizedText Text { get; set; } = new();
    }
}
