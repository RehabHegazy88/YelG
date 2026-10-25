using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YelG.WebSite.Application.Careers;

namespace YelG.WebSite.Infrastructure.Media
{
    /// <summary>
    /// CVs in a private folder (<see cref="CareersOptions.CvStoragePath"/>), under generated names in monthly
    /// folders. The folder is never mapped to a URL: CVs are read only by the admin panel's download action.
    /// </summary>
    public sealed class LocalCvStore : ICvStore
    {
        private readonly CareersOptions _options;
        private readonly ILogger<LocalCvStore> _logger;
        private readonly TimeProvider _clock;

        public LocalCvStore(IOptions<CareersOptions> options, ILogger<LocalCvStore> logger, TimeProvider clock)
        {
            _options = options.Value;
            _logger = logger;
            _clock = clock;
        }

        private string Root => Path.GetFullPath(_options.CvStoragePath);

        public async Task<string> SaveAsync(Stream content, string extension, CancellationToken ct = default)
        {
            var now = _clock.GetUtcNow().UtcDateTime;
            var relative = $"{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{extension}";
            var file = Resolve(relative)!;
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            await using (var output = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                await content.CopyToAsync(output, ct);
            return relative;
        }

        public Stream? OpenRead(string storedPath)
        {
            var file = Resolve(storedPath);
            return file is not null && File.Exists(file)
                ? new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true)
                : null;
        }

        public void Delete(string storedPath)
        {
            var file = Resolve(storedPath);
            try
            {
                if (file is not null && File.Exists(file)) File.Delete(file);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Could not delete CV {Path}", storedPath);
            }
        }

        /// <summary>The file for a stored path, or null when the path would leave the CV folder.</summary>
        private string? Resolve(string relativePath)
        {
            var root = Root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var file = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            return file.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? file : null;
        }
    }
}
