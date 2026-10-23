using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace YelG.BLL.Scoring
{
    /// <summary>
    /// Reads the first number in free text: Arabic-Indic and Eastern digits, "," and "٬" as thousands
    /// separators, "٫" as a decimal mark. "حوالي ١٢٬٥٠٠ جنيه" → 12500.
    /// </summary>
    public static class NumberParser
    {
        private static readonly Regex FirstNumber = new(@"-?\d+(?:\.\d+)?", RegexOptions.Compiled);

        public static bool TryParse(string? value, out double number)
        {
            number = 0;
            if (string.IsNullOrEmpty(value)) return false;

            var sb = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (ch >= '٠' && ch <= '٩') sb.Append((char)('0' + (ch - '٠')));
                else if (ch >= '۰' && ch <= '۹') sb.Append((char)('0' + (ch - '۰')));
                else if (ch == ',' || ch == '٬') continue;      // "," and Arabic thousands separator "٬"
                else if (ch == '٫') sb.Append('.');             // Arabic decimal separator "٫"
                else if ((ch >= '0' && ch <= '9') || ch == '.' || ch == '-') sb.Append(ch);
                else sb.Append(' ');
            }

            var match = FirstNumber.Match(sb.ToString());
            if (!match.Success) return false;
            number = double.Parse(match.Value, CultureInfo.InvariantCulture);
            return true;
        }

        /// <summary>The number, or 0 when the text has none (numericValue() semantics).</summary>
        public static double ValueOrZero(string? value) => TryParse(value, out var number) ? number : 0;
    }
}
