namespace YelG.WebSite.Domain.Common
{
    /// <summary>
    /// Same audit fields as the YELG Lens Basic entities: who added / last modified a row and when, and soft
    /// delete. Stamped by the Infrastructure interceptor, never by hand. The user ids are the admin users'
    /// ids; null means the seeder (or the system) wrote the row, so the Domain needs no Identity types.
    /// </summary>
    public abstract class BaseEntity
    {
        public long Id { get; set; }

        /// <summary>UTC.</summary>
        public DateTime AddedDate { get; set; }

        /// <summary>UTC.</summary>
        public DateTime LastModifiedDate { get; set; }

        public long? AddedBy_PersonID { get; set; }

        public long? ModifiedBy_PersonID { get; set; }

        public bool Deleted { get; set; }

        public DateTime? DeleteDate { get; set; }
    }

    /// <summary>A repeated item an editor can reorder and show or hide.</summary>
    public abstract class ContentItem : BaseEntity
    {
        public int SortOrder { get; set; }

        public bool IsPublished { get; set; } = true;
    }

    /// <summary>
    /// A homepage section (one row per section). Hiding a section hides it on the site and in the menus.
    /// </summary>
    public abstract class SectionBase : BaseEntity
    {
        public bool IsPublished { get; set; } = true;

        /// <summary>Small label above the heading ("من نحن").</summary>
        public LocalizedText Eyebrow { get; set; } = new();

        /// <summary>The section heading; may use *highlight* and a line break.</summary>
        public LocalizedText Heading { get; set; } = new();

        /// <summary>The line break in the heading is hidden on phones (br.hide-sm in the design).</summary>
        public bool HideHeadingBreakOnMobile { get; set; } = true;

        /// <summary>Introductory paragraph under the heading; empty when the section has none.</summary>
        public LocalizedText Lead { get; set; } = new();
    }
}
