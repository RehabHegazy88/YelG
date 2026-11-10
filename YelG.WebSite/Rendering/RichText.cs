using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;

namespace YelG.WebSite.Rendering
{
    public sealed record RichTextOptions
    {
        /// <summary>Wrap runs of Latin text in &lt;bdi class="lat"&gt; (Arabic pages), as the approved design does.</summary>
        public bool IsolateLatin { get; init; }

        /// <summary>Class of the line breaks, e.g. "hide-sm" for headings whose break is dropped on phones.</summary>
        public string? BreakClass { get; init; }

        /// <summary>Show a literal asterisk in the brand coral ("Fields marked * are required").</summary>
        public bool MarkStar { get; init; }
    }

    /// <summary>
    /// Renders the small markup editors may use, and nothing else: everything is HTML-encoded first.
    /// <list type="bullet">
    /// <item><c>*text*</c> highlights (&lt;em&gt;, coloured by the surrounding CSS);</item>
    /// <item>a newline is a line break;</item>
    /// <item><c>[label](#anchor)</c> is a link; only in-page anchors, https, mailto and tel links are kept;</item>
    /// <item><c>\*</c> is a literal asterisk.</item>
    /// </list>
    /// </summary>
    public static partial class RichText
    {
        // Arabic stays readable in the page source (only markup characters are escaped), like the rest of the site.
        private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(System.Text.Unicode.UnicodeRanges.All);

        // A Latin word or a run of Latin words ("YelG Advisory", "Infinite Financial Care", "ERP"); digits alone stay Arabic.
        [GeneratedRegex(@"[A-Za-z][A-Za-z0-9]*(?:[ &'’.\-][A-Za-z0-9]+)*")]
        private static partial Regex LatinRun();

        [GeneratedRegex(@"\G\[([^\]\n]+)\]\(([^)\s]+)\)")]
        private static partial Regex Link();

        public static IHtmlContent Render(string? text, RichTextOptions? options = null)
        {
            if (string.IsNullOrEmpty(text)) return HtmlString.Empty;
            var sb = new StringBuilder(text.Length + 32);
            Append(sb, text.Replace("\r\n", "\n"), options ?? new RichTextOptions(), allowLinks: true);
            return new HtmlString(sb.ToString());
        }

        /// <summary>Text without markup, e.g. for attributes and the screen-reader copy of a heading.</summary>
        public static string Plain(string? text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var noLinks = Regex.Replace(text, @"\[([^\]\n]+)\]\([^)\s]+\)", "$1");
            return noLinks.Replace("\\*", "\u0000").Replace("*", "").Replace("\u0000", "*").Replace("\r\n", " ").Replace('\n', ' ');
        }

        private static void Append(StringBuilder sb, string text, RichTextOptions o, bool allowLinks)
        {
            var plain = new StringBuilder();
            void Flush()
            {
                if (plain.Length == 0) return;
                AppendPlain(sb, plain.ToString(), o);
                plain.Clear();
            }

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == '\\' && i + 1 < text.Length && text[i + 1] == '*')
                {
                    Flush();
                    sb.Append(o.MarkStar ? "<span class=\"req\">*</span>" : "*");
                    i++;
                }
                else if (c == '*' && Closing(text, i + 1) is var end and > 0)
                {
                    Flush();
                    sb.Append("<em>");
                    Append(sb, text[(i + 1)..end], o, allowLinks);
                    sb.Append("</em>");
                    i = end;
                }
                else if (c == '[' && allowLinks && Link().Match(text, i) is { Success: true } m && SafeHref(m.Groups[2].Value))
                {
                    Flush();
                    var href = m.Groups[2].Value;
                    sb.Append("<a href=\"").Append(Encoder.Encode(href)).Append('"');
                    if (href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) sb.Append(" target=\"_blank\" rel=\"noopener\"");
                    sb.Append('>');
                    Append(sb, m.Groups[1].Value, o, allowLinks: false);
                    sb.Append("</a>");
                    i += m.Length - 1;
                }
                else if (c == '\n')
                {
                    Flush();
                    // The trailing space keeps the words apart when the break is hidden on phones;
                    // after a visible break a leading space is not rendered.
                    sb.Append(o.BreakClass is null ? "<br>" : $"<br class=\"{o.BreakClass}\"> ");
                }
                else
                {
                    plain.Append(c);
                }
            }
            Flush();
        }

        /// <summary>Index of the unescaped "*" that closes a highlight opened before <paramref name="from"/>, or -1.</summary>
        private static int Closing(string text, int from)
        {
            for (var j = from; j < text.Length; j++)
            {
                if (text[j] == '\\' && j + 1 < text.Length && text[j + 1] == '*') { j++; continue; }
                if (text[j] == '*') return j > from ? j : -1;
            }
            return -1;
        }

        private static bool SafeHref(string href) =>
            href.StartsWith('#') || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) || href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase);

        // The brand is always written "YelG": the span keeps it so inside labels the CSS sets in capitals.
        [GeneratedRegex(@"\bYelG\b")]
        private static partial Regex Brand();

        private static string Encode(string text) =>
            Brand().Replace(Encoder.Encode(text), "<span class=\"brand\">YelG</span>");

        private static void AppendPlain(StringBuilder sb, string text, RichTextOptions o)
        {
            if (!o.IsolateLatin)
            {
                sb.Append(Encode(text));
                return;
            }
            var last = 0;
            foreach (Match m in LatinRun().Matches(text))
            {
                sb.Append(Encode(text[last..m.Index]));
                sb.Append("<bdi class=\"lat\">").Append(Encode(m.Value)).Append("</bdi>");
                last = m.Index + m.Length;
            }
            sb.Append(Encode(text[last..]));
        }
    }
}
