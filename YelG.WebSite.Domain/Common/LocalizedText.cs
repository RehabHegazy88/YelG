namespace YelG.WebSite.Domain.Common
{
    /// <summary>
    /// One editable text in both languages. Stored as two columns of the owning table
    /// (an EF Core complex type: Heading_Ar, Heading_En). Empty strings, never null.
    /// </summary>
    /// <remarks>
    /// Text fields may carry a small markup the public site renders safely:
    /// <c>*highlight*</c>, a line break, and <c>[label](#anchor)</c> links. In Arabic, runs of Latin
    /// text are isolated automatically when rendered (bdi.lat), as the approved design does by hand.
    /// </remarks>
    public sealed class LocalizedText
    {
        public LocalizedText()
        {
        }

        public LocalizedText(string ar, string en)
        {
            Ar = ar;
            En = en;
        }

        public string Ar { get; set; } = string.Empty;

        public string En { get; set; } = string.Empty;

        /// <summary>The text for a culture name such as "ar" or "en"; Arabic is the default.</summary>
        public string For(string culture) =>
            culture.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? En : Ar;

        public bool IsEmpty => string.IsNullOrWhiteSpace(Ar) && string.IsNullOrWhiteSpace(En);

        public override string ToString() => Ar;
    }
}
