using YelG.WebSite.Application.Content;
using YelG.WebSite.Domain.Sections;
using YelG.WebSite.Localization;

namespace YelG.WebSite.Models
{
    /// <summary>All news and insights (/{lang}/insights), newest first, with a category filter.</summary>
    public sealed class InsightsPageModel : HomePageModel
    {
        public InsightsPageModel(HomeContent content, string lang, UiStrings ui, string baseUrl, string? category)
            : base(content, lang, ui, preview: false, baseUrl)
        {
            Category = category;
            Posts = content.InsightPosts.OrderByDescending(p => p.PublishedOn).ThenBy(p => p.SortOrder).ToList();
        }

        /// <summary>The category chosen (its label in this language); null for all.</summary>
        public string? Category { get; }

        public IReadOnlyList<InsightPost> Posts { get; }

        public IReadOnlyList<string> Categories =>
            Posts.Select(p => p.CategoryLabel.For(Lang)).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();

        public IReadOnlyList<InsightPost> ListedPosts =>
            string.IsNullOrEmpty(Category) ? Posts : Posts.Where(p => p.CategoryLabel.For(Lang) == Category).ToList();

        public override string PagePath => "/insights";

        public override string? PageTitle => $"{I("pageTitle")} — YelG Advisory";

        public override string? PageDescription => Content.Insights is { } section && !section.Lead.IsEmpty ? T(section.Lead) : null;

        /// <summary>A label of this page in this language.</summary>
        public string I(string key) => InsightTexts.Get(key, Lang);

        public string ListUrl => $"/{Lang}/insights";
    }
}
