using System.Globalization;

namespace YelG.WebSite.Localization
{
    /// <summary>
    /// The site's two languages and the cultures behind them. Arabic uses ar-EG: Gregorian months
    /// ("سبتمبر 2026"), where the neutral "ar" culture would format dates with the Hijri calendar.
    /// </summary>
    public static class SiteLanguage
    {
        public const string Arabic = "ar";
        public const string English = "en";

        /// <summary>Route constraint for the {lang} segment.</summary>
        public const string RouteConstraint = "regex(^(ar|en)$)";

        public static readonly IReadOnlyList<string> All = [Arabic, English];

        public static bool IsSupported(string? lang) => lang is Arabic or English;

        public static CultureInfo Culture(string lang) =>
            CultureInfo.GetCultureInfo(lang == English ? "en-US" : "ar-EG");

        /// <summary>The language of the current request (set from the route by the localization middleware).</summary>
        public static string Current =>
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == English ? English : Arabic;

        public static string Other(string lang) => lang == English ? Arabic : English;

        /// <summary>Open Graph locale, e.g. "ar_EG".</summary>
        public static string OgLocale(string lang) => Culture(lang).Name.Replace('-', '_');
    }
}
