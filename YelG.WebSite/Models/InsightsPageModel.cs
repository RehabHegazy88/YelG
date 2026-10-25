using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;
using YelG.WebSite.Application.Content;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Sections;
using YelG.WebSite.Localization;
using YelG.WebSite.Rendering;

namespace YelG.WebSite.Models
{
    /// <summary>
    /// All news and insights (/{lang}/insights), newest first, with a category filter; or one post's own page
    /// (/{lang}/insights/{slug}).
    /// </summary>
    public sealed partial class InsightsPageModel : HomePageModel
    {
        public InsightsPageModel(HomeContent content, string lang, UiStrings ui, string baseUrl, string? category, InsightPost? post = null)
            : base(content, lang, ui, preview: false, baseUrl)
        {
            Category = category;
            Post = post;
            Posts = content.InsightPosts.OrderByDescending(p => p.PublishedOn).ThenBy(p => p.SortOrder).ToList();
        }

        /// <summary>The category chosen (its label in this language); null for all.</summary>
        public string? Category { get; }

        /// <summary>The post of a post's page; null on the list.</summary>
        public InsightPost? Post { get; }

        public IReadOnlyList<InsightPost> Posts { get; }

        public IReadOnlyList<string> Categories =>
            Posts.Select(p => p.CategoryLabel.For(Lang)).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();

        public IReadOnlyList<InsightPost> ListedPosts =>
            string.IsNullOrEmpty(Category) ? Posts : Posts.Where(p => p.CategoryLabel.For(Lang) == Category).ToList();

        /// <summary>Other posts for the foot of a post's page: the same category first, then the newest.</summary>
        public IReadOnlyList<InsightPost> MorePosts => Post is null ? [] : Posts.Where(p => p.Id != Post.Id)
            .OrderByDescending(p => p.CategoryLabel.For(Lang) == Post.CategoryLabel.For(Lang)).ThenByDescending(p => p.PublishedOn).Take(3).ToList();

        public override string PagePath => Post is null ? "/insights" : $"/insights/{Post.Slug}";

        public override string? PageTitle => Post is null
            ? $"{I("pageTitle")} — YelG Advisory"
            : $"{RichText.Plain(T(Post.Title))} — YelG Advisory";

        public override string? PageDescription => Post is not null
            ? (!Post.Summary.IsEmpty ? RichText.Plain(T(Post.Summary)) : null)
            : Content.Insights is { } section && !section.Lead.IsEmpty ? T(section.Lead) : null;

        public override string? PageImage => Post?.Image?.Path;

        /// <summary>A label of this page in this language.</summary>
        public string I(string key) => InsightTexts.Get(key, Lang);

        public string ListUrl => $"/{Lang}/insights";

        /// <summary>Where a post's card goes: its page on this site, else where it was published.</summary>
        public static string Link(InsightPost post, string lang) => post.HasPage ? $"/{lang}/insights/{post.Slug}" : post.ExternalUrl;

        public string Date(DateOnly date) => date.ToString("d MMMM yyyy", Culture);

        /// <summary>"Read on LinkedIn".</summary>
        public string ReadOn(InsightPost post) => I("readOn").Replace("{source}", string.IsNullOrWhiteSpace(post.Source) ? I("source") : post.Source);

        /// <summary>
        /// The article in blocks: paragraphs (split at an empty line) and "## " subheadings (one line; the lines
        /// after it are a paragraph).
        /// </summary>
        public IReadOnlyList<(bool IsHeading, IHtmlContent Html)> Blocks(LocalizedText body)
        {
            var blocks = new List<(bool, IHtmlContent)>();
            var parts = ParagraphBreak().Split(body.For(Lang).Replace("\r\n", "\n").Trim()).Select(b => b.Trim()).Where(b => b.Length > 0);
            foreach (var part in parts)
            {
                var text = part;
                if (text.StartsWith("## ", StringComparison.Ordinal))
                {
                    var end = text.IndexOf('\n');
                    var heading = (end < 0 ? text[3..] : text[3..end]).Trim();
                    blocks.Add((true, R(new LocalizedText(heading, heading))));
                    text = end < 0 ? "" : text[(end + 1)..].Trim();
                }
                if (text.Length > 0) blocks.Add((false, R(new LocalizedText(text, text))));
            }
            return blocks;
        }

        [GeneratedRegex(@"\n\s*\n")]
        private static partial Regex ParagraphBreak();
    }
}
