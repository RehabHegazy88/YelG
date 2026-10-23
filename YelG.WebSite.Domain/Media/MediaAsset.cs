using YelG.WebSite.Domain.Common;

namespace YelG.WebSite.Domain.Media
{
    public enum MediaKind
    {
        Image = 0,
        Video = 1
    }

    /// <summary>An uploaded image or video. Editors pick these in every section that shows media.</summary>
    public class MediaAsset : BaseEntity
    {
        public MediaKind Kind { get; set; }

        /// <summary>Name as uploaded, for the library list.</summary>
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>Public path under wwwroot, e.g. "/assets/meeting.jpg" or "/media/2026/10/….webp".</summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>Generated WebP thumbnail for the library; null for videos.</summary>
        public string? ThumbnailPath { get; set; }

        public string ContentType { get; set; } = string.Empty;

        public long SizeBytes { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        /// <summary>Default alternative text; a section can override it where the image is used.</summary>
        public LocalizedText Alt { get; set; } = new();
    }
}
