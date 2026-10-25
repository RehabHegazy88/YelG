using System.Globalization;
using Microsoft.AspNetCore.Html;
using YelG.WebSite.Application.Content;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Media;
using YelG.WebSite.Localization;
using YelG.WebSite.Rendering;

namespace YelG.WebSite.Models
{
    /// <summary>The homepage in one language, with the helpers its partial views use.</summary>
    public sealed class HomePageModel
    {
        public HomePageModel(HomeContent content, string lang, UiStrings ui, bool preview, string baseUrl)
        {
            Content = content;
            Lang = lang;
            Ui = ui;
            IsPreview = preview;
            BaseUrl = baseUrl.TrimEnd('/');
        }

        public HomeContent Content { get; }

        /// <summary>"ar" or "en".</summary>
        public string Lang { get; }

        public bool IsArabic => Lang == SiteLanguage.Arabic;

        public string Dir => IsArabic ? "rtl" : "ltr";

        public UiStrings Ui { get; }

        /// <summary>The editor's preview: hidden sections are shown and the page is not indexed.</summary>
        public bool IsPreview { get; }

        /// <summary>Scheme and host of the public site, for canonical and alternate links.</summary>
        public string BaseUrl { get; }

        public CultureInfo Culture => SiteLanguage.Culture(Lang);

        /// <summary>The plain text of a field in this language (markup removed), for attributes and titles.</summary>
        public string T(LocalizedText? text) => RichText.Plain(text?.For(Lang));

        /// <summary>A text field with its markup; Latin terms are isolated on the Arabic page.</summary>
        public IHtmlContent R(LocalizedText? text, string? breakClass = null, bool markStar = false) =>
            RichText.Render(text?.For(Lang), new RichTextOptions { IsolateLatin = IsArabic, BreakClass = breakClass, MarkStar = markStar });

        /// <summary>
        /// A text kept as typed (the design shows the services, the self-check and the form options without
        /// Latin isolation, as they come from its data lists).
        /// </summary>
        public IHtmlContent Data(LocalizedText? text) => RichText.Render(text?.For(Lang));

        /// <summary>A section heading: its line break is hidden on phones when the section says so.</summary>
        public IHtmlContent Heading(SectionBase section) => R(section.Heading, section.HideHeadingBreakOnMobile ? "hide-sm" : null);

        /// <summary>Fixed interface wording.</summary>
        public string S(string key) => Ui.Get(key, Lang);

        /// <summary>Fixed interface wording with Latin terms isolated on the Arabic page.</summary>
        public IHtmlContent SR(string key) => RichText.Render(Ui.Get(key, Lang), new RichTextOptions { IsolateLatin = IsArabic });

        /// <summary>A plain value (an email, a source name) with Latin isolation on the Arabic page.</summary>
        public IHtmlContent Iso(string? value) => RichText.Render(value?.Replace("*", "\\*"), new RichTextOptions { IsolateLatin = IsArabic });

        public string Url(string lang) => $"{BaseUrl}/{lang}";

        public string OtherLang => SiteLanguage.Other(Lang);

        /// <summary>The language switch: the other language's page, or its preview while previewing.</summary>
        public string LangSwitchUrl => IsPreview ? $"/admin/preview/{OtherLang}" : $"/{OtherLang}";

        /// <summary>"+20 10 0667 7363" → "tel:+201006677363".</summary>
        public static string TelHref(string phone) => "tel:" + new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());

        /// <summary>"https://www.yelgadvisory.com/" → "www.yelgadvisory.com".</summary>
        public static string DisplayUrl(string url) =>
            url.Replace("https://", "", StringComparison.OrdinalIgnoreCase).Replace("http://", "", StringComparison.OrdinalIgnoreCase).TrimEnd('/');

        /// <summary>
        /// How a picture is placed when it fills a cropped frame: tall and square pictures keep their top
        /// (faces, headlines), wide ones stay centred, as in the design.
        /// </summary>
        public static string FocusClass(MediaAsset? media) =>
            media is { Width: > 0, Height: > 0 } && media.Height >= media.Width ? "focus-top" : "";

        public string Year => DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);

        /// <summary>"سبتمبر 2026" / "Sep 2026", as the two designs write post dates.</summary>
        public string MonthYear(DateOnly date) => date.ToString(IsArabic ? "MMMM yyyy" : "MMM yyyy", Culture);

        /// <summary>The "→" of a call to action, mirrored on the Arabic page.</summary>
        public string ArrowNext => S("arrowNext");

        public string ArrowExternal => S("arrowExternal");

        /// <summary>The arrow in the round part of a button: down-left on the Arabic page, up-right on the English one.</summary>
        public IHtmlContent ArrowIcon => new HtmlString(
            "<svg aria-hidden=\"true\" width=\"16\" height=\"16\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\"><path d=\""
            + (IsArabic ? "M17 7 7 17M7 8v9h9" : "M7 17 17 7M8 7h9v9") + "\"></path></svg>");
    }
}
