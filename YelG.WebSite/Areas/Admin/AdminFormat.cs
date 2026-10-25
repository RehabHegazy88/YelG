using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;

namespace YelG.WebSite.Areas.Admin
{
    /// <summary>How the admin panel writes dates and actions.</summary>
    public static partial class AdminFormat
    {
        /// <summary>
        /// A help text with its Latin parts (#consult, https://, +20 10 …, {year}) isolated left to right, so
        /// they keep their order inside the Arabic sentence.
        /// </summary>
        public static IHtmlContent Hint(string? text)
        {
            if (string.IsNullOrEmpty(text)) return HtmlString.Empty;
            var clean = text.Replace("\u200E", "");   // left-to-right marks typed in the hints are not needed
            var html = new System.Text.StringBuilder();
            var at = 0;
            foreach (Match m in LatinRun().Matches(clean))
            {
                var run = m.Value.TrimEnd('.');   // the sentence's full stop stays outside
                if (run.Length == 0) continue;
                html.Append(WebUtility.HtmlEncode(clean[at..m.Index]))
                    .Append("<bdi dir=\"ltr\">").Append(WebUtility.HtmlEncode(run)).Append("</bdi>");
                at = m.Index + run.Length;
            }
            html.Append(WebUtility.HtmlEncode(clean[at..]));
            return new HtmlString(html.ToString());
        }

        [GeneratedRegex(@"[A-Za-z0-9#:/._@{}+*\-]+(?: [A-Za-z0-9#:/._@{}+*\-]+)*")]
        private static partial Regex LatinRun();

        private static readonly TimeZoneInfo Cairo = FindCairo();

        private static TimeZoneInfo FindCairo()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Utc;
            }
        }

        /// <summary>A stored UTC time in Cairo time, "2026-10-24 14:05".</summary>
        public static string Time(DateTime? utc) =>
            utc is null || utc == default
                ? "—"
                : TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc), Cairo)
                    .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        public static string Action(string action) => action switch
        {
            "Created" => "إضافة",
            "Updated" => "تعديل",
            "Deleted" => "حذف",
            "Published" => "إظهار",
            "Hidden" => "إخفاء",
            "Reordered" => "ترتيب",
            "Seeded" => "تحميل أولي",
            "Exported" => "تصدير",
            "Downloaded" => "تحميل",
            _ => action
        };

        public static string Role(string role) => role switch
        {
            "Admin" => "مدير",
            "Editor" => "محرر",
            _ => "—"
        };
    }
}
