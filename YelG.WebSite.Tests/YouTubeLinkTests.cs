using YelG.WebSite.Domain.Common;

namespace YelG.WebSite.Tests
{
    public class YouTubeLinkTests
    {
        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
        [InlineData("https://youtube.com/watch?feature=share&v=dQw4w9WgXcQ&t=30", "dQw4w9WgXcQ")]
        [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
        [InlineData("https://youtu.be/dQw4w9WgXcQ?si=abc", "dQw4w9WgXcQ")]
        [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
        [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
        [InlineData("https://www.youtube.com/live/dQw4w9WgXcQ?feature=shared", "dQw4w9WgXcQ")]
        [InlineData("https://www.youtube.com/@yelgadvisory", null)]                 // a channel, not a video
        [InlineData("https://www.youtube.com/watch?v=short", null)]               // not an id
        [InlineData("https://evil.example/watch?v=dQw4w9WgXcQ", null)]           // not YouTube
        [InlineData("https://youtube.com.evil.example/watch?v=dQw4w9WgXcQ", null)]
        [InlineData("javascript:alert(1)", null)]
        [InlineData("", null)]
        public void The_video_id_is_read_only_from_youtube_video_links(string url, string? id) =>
            Assert.Equal(id, YouTubeLink.VideoId(url));
    }
}
