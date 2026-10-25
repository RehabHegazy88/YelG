
namespace YelG.WebSite.Localization
{
    /// <summary>
    /// Wording of the news and insights page (/{lang}/insights), which is not in the approved design. The page's
    /// heading and lead are the insights section's own (edited in the admin panel). All need the company's approval.
    /// </summary>
    public static class InsightTexts
    {
        private static readonly Dictionary<string, (string Ar, string En)> Texts = new()
        {
            ["pageTitle"] = ("الأخبار والمقالات", "News & insights"),
            ["viewAll"] = ("كل الأخبار والمقالات", "All news & insights"),
            ["all"] = ("الكل", "All"),
            ["filterLabel"] = ("تصنيف المقالات", "Filter by category"),
            ["readOn"] = ("اقرأ على {source}", "Read on {source}"),
            ["latest"] = ("الأحدث", "Latest"),
            ["empty"] = ("لا توجد مقالات في هذا التصنيف بعد.", "Nothing in this category yet."),
            ["count"] = ("{n} مقال", "{n} posts"),
            ["source"] = ("الموقع الأصلي", "the original site"),
            ["alsoOn"] = ("نُشر هذا الخبر أيضًا على {source}", "Also published on {source}"),
            ["more"] = ("أخبار ومقالات أخرى", "More news & insights"),
            ["readMore"] = ("اقرأ الخبر", "Read more"),
            ["video"] = ("فيديو", "Video")
        };

        public static string Get(string key, string lang) =>
            Texts.TryGetValue(key, out var t) ? (lang == SiteLanguage.English ? t.En : t.Ar) : key;
    }
}
