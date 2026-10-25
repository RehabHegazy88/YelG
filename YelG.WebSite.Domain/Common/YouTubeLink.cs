using System.Text.RegularExpressions;

namespace YelG.WebSite.Domain.Common
{
    /// <summary>
    /// Reads the video id from a YouTube link: youtube.com/watch?v=…, youtu.be/…, youtube.com/shorts/…,
    /// /embed/… and /live/… (with or without "www." or "m."). Anything else is not a YouTube video.
    /// </summary>
    public static partial class YouTubeLink
    {
        public static string? VideoId(string? url)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)) return null;
            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return null;

            var host = uri.Host.ToLowerInvariant();
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            string? id = null;
            if (host == "youtu.be")
                id = segments.FirstOrDefault();
            else if (host is "youtube.com" or "www.youtube.com" or "m.youtube.com" or "www.youtube-nocookie.com")
            {
                if (segments is ["watch"])
                    id = Query(uri.Query, "v");
                else if (segments is [("shorts" or "embed" or "live"), var value, ..])
                    id = value;
            }
            return id is not null && IdPattern().IsMatch(id) ? id : null;
        }

        private static string? Query(string query, string name) =>
            query.TrimStart('?').Split('&').Select(p => p.Split('=', 2))
                .FirstOrDefault(p => p.Length == 2 && p[0] == name)?[1];

        [GeneratedRegex("^[A-Za-z0-9_-]{11}$")]
        private static partial Regex IdPattern();
    }
}
