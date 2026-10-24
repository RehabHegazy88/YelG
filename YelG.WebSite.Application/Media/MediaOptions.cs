namespace YelG.WebSite.Application.Media
{
    /// <summary>"Media" configuration: where uploads are kept and what is accepted.</summary>
    public sealed class MediaOptions
    {
        public const string SectionName = "Media";

        /// <summary>
        /// Folder on disk for uploads, outside the deployed files so a deployment never removes them.
        /// Relative paths are under the content root. Default: App_Data/media.
        /// </summary>
        public string StoragePath { get; set; } = Path.Combine("App_Data", "media");

        /// <summary>URL prefix the uploads are served under.</summary>
        public string RequestPath { get; set; } = "/media";

        public long MaxImageBytes { get; set; } = 10 * 1024 * 1024;

        public long MaxVideoBytes { get; set; } = 200 * 1024 * 1024;

        /// <summary>Width of the generated WebP thumbnails (the height keeps the proportions).</summary>
        public int ThumbnailWidth { get; set; } = 480;
    }
}
