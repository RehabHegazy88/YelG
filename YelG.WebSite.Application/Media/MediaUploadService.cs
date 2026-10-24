using Microsoft.Extensions.Options;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Domain.Media;

namespace YelG.WebSite.Application.Media
{
    /// <summary>
    /// Adds an uploaded file to the media library: checks its real type and size, keeps the original under a
    /// generated name, writes a WebP thumbnail for images and records the asset. Messages are for the
    /// (Arabic) admin panel.
    /// </summary>
    public sealed class MediaUploadService
    {
        private readonly IContentStore _store;
        private readonly IMediaFileStore _files;
        private readonly IImageProcessor _images;
        private readonly MediaOptions _options;
        private readonly TimeProvider _clock;

        public MediaUploadService(IContentStore store, IMediaFileStore files, IImageProcessor images,
            IOptions<MediaOptions> options, TimeProvider clock)
        {
            _store = store;
            _files = files;
            _images = images;
            _options = options.Value;
            _clock = clock;
        }

        /// <param name="content">The uploaded file; must be seekable (ASP.NET Core buffers form files).</param>
        public async Task<MediaUploadResult> UploadAsync(Stream content, string fileName, CancellationToken ct = default)
        {
            if (!content.CanSeek) throw new ArgumentException("The upload stream must be seekable.", nameof(content));
            if (content.Length == 0) return MediaUploadResult.Fail("الملف فارغ.");

            var header = new byte[MediaFileType.HeaderLength];
            var read = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
            content.Position = 0;
            var type = MediaFileType.Detect(header.AsSpan(0, read));
            if (type is null)
                return MediaUploadResult.Fail("نوع الملف غير مدعوم. المسموح: JPG وPNG وWebP وGIF للصور، وMP4 وWebM للفيديو.");

            var limit = type.Kind == MediaKind.Image ? _options.MaxImageBytes : _options.MaxVideoBytes;
            if (content.Length > limit)
                return MediaUploadResult.Fail($"حجم الملف أكبر من المسموح ({FormatSize(limit)}).");

            ProcessedImage? image = null;
            if (type.Kind == MediaKind.Image)
            {
                image = _images.Process(content, _options.ThumbnailWidth);
                content.Position = 0;
                if (image is null) return MediaUploadResult.Fail("تعذّرت قراءة الصورة، أو أبعادها كبيرة جدًا.");
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var name = $"{now:yyyy}/{now:MM}/{Guid.NewGuid():N}";
            var written = new List<string>();
            try
            {
                var path = await _files.SaveAsync(content, name + type.Extension, ct);
                written.Add(path);
                string? thumbnail = null;
                if (image is not null)
                {
                    using var thumb = new MemoryStream(image.ThumbnailWebP);
                    thumbnail = await _files.SaveAsync(thumb, name + ".thumb.webp", ct);
                    written.Add(thumbnail);
                }

                var asset = new MediaAsset
                {
                    Kind = type.Kind,
                    OriginalFileName = CleanFileName(fileName),
                    Path = path,
                    ThumbnailPath = thumbnail,
                    ContentType = type.ContentType,
                    SizeBytes = content.Length,
                    Width = image?.Width,
                    Height = image?.Height
                };
                _store.Add(asset);
                await _store.SaveChangesAsync(ct);
                return new MediaUploadResult(asset, null);
            }
            catch
            {
                foreach (var path in written) _files.TryDelete(path);
                throw;
            }
        }

        private static string CleanFileName(string fileName)
        {
            var name = Path.GetFileName(fileName.Replace('\\', '/')).Trim();
            if (name.Length == 0) name = "file";
            return name.Length > 260 ? name[^260..] : name;
        }

        public static string FormatSize(long bytes) =>
            bytes >= 1024 * 1024 ? $"{bytes / (1024d * 1024):0.#} MB" : $"{Math.Max(1, bytes / 1024)} KB";
    }

    public sealed record MediaUploadResult(MediaAsset? Asset, string? Error)
    {
        public bool Succeeded => Asset is not null;

        public static MediaUploadResult Fail(string error) => new(null, error);
    }
}
