using SkiaSharp;
using YelG.WebSite.Application.Media;

namespace YelG.WebSite.Infrastructure.Media
{
    /// <summary>
    /// Decodes uploads with SkiaSharp (MIT; native assets for Windows and Linux): the size as displayed
    /// (camera rotation applied) and a WebP thumbnail. Very large images are refused before decoding.
    /// </summary>
    public sealed class SkiaImageProcessor : IImageProcessor
    {
        /// <summary>About 50 megapixels (e.g. 8660 × 5770).</summary>
        private const long MaxPixels = 50_000_000;

        public ProcessedImage? Process(Stream image, int thumbnailWidth)
        {
            // SKCodec.Create(Stream) takes the stream and closes it; the caller still needs it, so decode a copy
            using var data = SKData.Create(image);
            if (data is null) return null;
            using var codec = SKCodec.Create(data);
            if (codec is null) return null;
            var info = codec.Info;
            if (info.Width <= 0 || info.Height <= 0 || (long)info.Width * info.Height > MaxPixels) return null;

            using var decoded = SKBitmap.Decode(codec);
            if (decoded is null) return null;
            using var bitmap = Orient(decoded, codec.EncodedOrigin);

            var width = Math.Min(thumbnailWidth, bitmap.Width);
            var height = Math.Max(1, (int)Math.Round(bitmap.Height * (double)width / bitmap.Width));
            using var thumb = bitmap.Resize(new SKImageInfo(width, height), SKFilterQuality.High) ?? bitmap.Copy();
            using var encoded = SKImage.FromBitmap(thumb).Encode(SKEncodedImageFormat.Webp, 80);
            return new ProcessedImage(bitmap.Width, bitmap.Height, encoded.ToArray());
        }

        /// <summary>A copy turned upright for photos stored sideways (EXIF orientation).</summary>
        private static SKBitmap Orient(SKBitmap source, SKEncodedOrigin origin)
        {
            var degrees = origin switch
            {
                SKEncodedOrigin.BottomRight or SKEncodedOrigin.BottomLeft => 180,
                SKEncodedOrigin.RightTop or SKEncodedOrigin.LeftTop => 90,
                SKEncodedOrigin.LeftBottom or SKEncodedOrigin.RightBottom => 270,
                _ => 0
            };
            if (degrees == 0) return source.Copy();

            var swap = degrees != 180;
            var result = new SKBitmap(swap ? source.Height : source.Width, swap ? source.Width : source.Height);
            using var canvas = new SKCanvas(result);
            canvas.Translate(result.Width / 2f, result.Height / 2f);
            canvas.RotateDegrees(degrees);
            canvas.Translate(-source.Width / 2f, -source.Height / 2f);
            canvas.DrawBitmap(source, 0, 0);
            return result;
        }
    }
}
