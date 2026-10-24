using YelG.WebSite.Domain.Media;

namespace YelG.WebSite.Application.Media
{
    /// <summary>
    /// The accepted upload formats, recognised by their first bytes rather than the name or the browser's
    /// claim. SVG is not accepted: it can carry script.
    /// </summary>
    public sealed record MediaFileType(MediaKind Kind, string ContentType, string Extension)
    {
        public static readonly MediaFileType Jpeg = new(MediaKind.Image, "image/jpeg", ".jpg");
        public static readonly MediaFileType Png = new(MediaKind.Image, "image/png", ".png");
        public static readonly MediaFileType WebP = new(MediaKind.Image, "image/webp", ".webp");
        public static readonly MediaFileType Gif = new(MediaKind.Image, "image/gif", ".gif");
        public static readonly MediaFileType Mp4 = new(MediaKind.Video, "video/mp4", ".mp4");
        public static readonly MediaFileType WebM = new(MediaKind.Video, "video/webm", ".webm");

        /// <summary>For the file input's accept attribute.</summary>
        public const string Accept = "image/jpeg,image/png,image/webp,image/gif,video/mp4,video/webm";

        /// <summary>Bytes needed by <see cref="Detect"/>.</summary>
        public const int HeaderLength = 16;

        public static MediaFileType? Detect(ReadOnlySpan<byte> h)
        {
            if (h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return Jpeg;
            if (h.Length >= 8 && h[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return Png;
            if (h.Length >= 12 && h[..4].SequenceEqual("RIFF"u8) && h[8..12].SequenceEqual("WEBP"u8)) return WebP;
            if (h.Length >= 6 && (h[..6].SequenceEqual("GIF87a"u8) || h[..6].SequenceEqual("GIF89a"u8))) return Gif;
            if (h.Length >= 12 && h[4..8].SequenceEqual("ftyp"u8)) return Mp4;
            if (h.Length >= 4 && h[0] == 0x1A && h[1] == 0x45 && h[2] == 0xDF && h[3] == 0xA3) return WebM;
            return null;
        }
    }
}
