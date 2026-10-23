using Microsoft.Extensions.Caching.Memory;

namespace YelG.WebSite.Application.Content
{
    /// <summary>The homepage content, cached in memory until the next content save.</summary>
    public sealed class HomeContentService
    {
        private static readonly TimeSpan Lifetime = TimeSpan.FromHours(6);

        private readonly IHomeContentReader _reader;
        private readonly IMemoryCache _cache;
        private readonly IContentVersion _version;

        public HomeContentService(IHomeContentReader reader, IMemoryCache cache, IContentVersion version)
        {
            _reader = reader;
            _cache = cache;
            _version = version;
        }

        /// <param name="preview">Includes hidden sections and items, and is never cached.</param>
        public async Task<HomeContent> GetAsync(bool preview = false, CancellationToken cancellationToken = default)
        {
            if (preview) return await _reader.ReadAsync(includeHidden: true, cancellationToken);

            // read the version before loading: a save during the load leaves this entry under the old key
            var key = ("home-content", _version.Current);
            if (_cache.TryGetValue(key, out HomeContent? cached) && cached is not null) return cached;

            var content = await _reader.ReadAsync(includeHidden: false, cancellationToken);
            _cache.Set(key, content, Lifetime);
            return content;
        }
    }
}
