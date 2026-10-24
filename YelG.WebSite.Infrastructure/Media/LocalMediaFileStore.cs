using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YelG.WebSite.Application.Media;

namespace YelG.WebSite.Infrastructure.Media
{
    /// <summary>Uploads in a folder on the server (<see cref="MediaOptions.StoragePath"/>), served under <see cref="MediaOptions.RequestPath"/>.</summary>
    public sealed class LocalMediaFileStore : IMediaFileStore
    {
        private readonly MediaOptions _options;
        private readonly ILogger<LocalMediaFileStore> _logger;

        public LocalMediaFileStore(IOptions<MediaOptions> options, ILogger<LocalMediaFileStore> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        private string Root => Path.GetFullPath(_options.StoragePath);

        public async Task<string> SaveAsync(Stream content, string relativePath, CancellationToken ct = default)
        {
            var file = Resolve(relativePath) ?? throw new ArgumentException("Invalid media path.", nameof(relativePath));
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            await using (var output = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                await content.CopyToAsync(output, ct);
            return $"{_options.RequestPath.TrimEnd('/')}/{relativePath}";
        }

        public void TryDelete(string publicPath)
        {
            var prefix = _options.RequestPath.TrimEnd('/') + "/";
            if (!publicPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;
            var file = Resolve(publicPath[prefix.Length..]);
            try
            {
                if (file is not null && File.Exists(file)) File.Delete(file);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Could not delete media file {Path}", publicPath);
            }
        }

        /// <summary>The file for a relative path, or null when the path would leave the media folder.</summary>
        private string? Resolve(string relativePath)
        {
            var root = Root;
            var file = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            return file.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                ? file : null;
        }
    }
}
