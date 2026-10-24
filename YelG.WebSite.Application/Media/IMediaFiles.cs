namespace YelG.WebSite.Application.Media
{
    /// <summary>Where uploaded files are written (Infrastructure: a folder on the server).</summary>
    public interface IMediaFileStore
    {
        /// <summary>Writes a file under the media folder and returns its public path ("/media/2026/10/….jpg").</summary>
        /// <param name="relativePath">Forward slashes, e.g. "2026/10/3f2a….jpg".</param>
        Task<string> SaveAsync(Stream content, string relativePath, CancellationToken ct = default);

        /// <summary>Removes a file written by <see cref="SaveAsync"/> (used when an upload fails half way).</summary>
        void TryDelete(string publicPath);
    }

    /// <summary>Reads images and makes thumbnails (Infrastructure).</summary>
    public interface IImageProcessor
    {
        /// <summary>The image's size and a WebP thumbnail of the given width; null when it cannot be decoded.</summary>
        ProcessedImage? Process(Stream image, int thumbnailWidth);
    }

    public sealed record ProcessedImage(int Width, int Height, byte[] ThumbnailWebP);
}
