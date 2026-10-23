using System.Text.RegularExpressions;

namespace YelG.BLL.Common
{
    public static class Text
    {
        private static readonly Regex ControlChars = new(@"[\u0000-\u001F\u007F]", RegexOptions.Compiled);
        private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);

        /// <summary>Removes control characters, trims, collapses spaces and cuts to <paramref name="max"/> (cleanText_).</summary>
        public static string Clean(string? value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var cleaned = Spaces.Replace(ControlChars.Replace(value, string.Empty).Trim(), " ");
            return cleaned.Length > max ? cleaned[..max] : cleaned;
        }

        /// <summary>Like <see cref="Clean"/> but keeps line breaks (notes).</summary>
        public static string CleanMultiline(string? value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var cleaned = Regex.Replace(value, @"[\u0000-\u0009\u000B\u000C\u000E-\u001F\u007F]", string.Empty).Trim();
            return cleaned.Length > max ? cleaned[..max] : cleaned;
        }
    }
}
