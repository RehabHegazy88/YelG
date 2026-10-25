using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Media;

namespace YelG.WebSite.Areas.Admin.Editing
{
    /// <summary>Messages per form field ("Heading.Ar", "Key") and for the whole form ("").</summary>
    public sealed class EditErrors
    {
        private readonly Dictionary<string, string> _errors = new(StringComparer.Ordinal);

        public void Add(string field, string message) => _errors.TryAdd(field, message);

        public string? this[string field] => _errors.GetValueOrDefault(field);

        public bool Any => _errors.Count > 0;

        public int Count => _errors.Count;

        public IEnumerable<string> Messages => _errors.Values;
    }

    /// <summary>Reads posted form values into an entity's properties and checks them against the field rules.</summary>
    public static partial class FieldBinder
    {
        public const string Ar = "Ar";
        public const string En = "En";

        public static async Task BindAsync(FieldDef f, object entity, IFormCollection form, EditErrors errors,
            IReadOnlyList<FieldOption> options, IContentStore store, CancellationToken ct)
        {
            if (f.IsLocalized)
            {
                var text = (LocalizedText)f.Property.GetValue(entity)!;
                text.Ar = Clean(form[$"{f.Name}.{Ar}"], f.Kind == FieldKind.TextArea);
                text.En = Clean(form[$"{f.Name}.{En}"], f.Kind == FieldKind.TextArea);
                CheckText(f, $"{f.Name}.{Ar}", text.Ar, "بالعربية", errors);
                CheckText(f, $"{f.Name}.{En}", text.En, "بالإنجليزية", errors);
                return;
            }

            var raw = Clean(form[f.Name], multiline: false);
            switch (f.Kind)
            {
                case FieldKind.Bool:
                    f.Property.SetValue(entity, form[f.Name].Contains("true"));
                    return;

                case FieldKind.Int:
                    if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                    {
                        errors.Add(f.Name, "أدخل رقمًا صحيحًا.");
                        return;
                    }
                    if (number < (f.Min ?? int.MinValue) || number > (f.Max ?? int.MaxValue))
                        errors.Add(f.Name, $"القيمة من {f.Min} إلى {f.Max}.");
                    f.Property.SetValue(entity, number);
                    return;

                case FieldKind.Date:
                    if (raw.Length == 0 && f.IsNullable)
                    {
                        f.Property.SetValue(entity, null);
                        return;
                    }
                    if (!DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    {
                        errors.Add(f.Name, "أدخل تاريخًا صحيحًا.");
                        return;
                    }
                    f.Property.SetValue(entity, date);
                    return;

                case FieldKind.Select:
                    if (raw.Length == 0)
                    {
                        if (f.EmptyLabel is not null && f.IsNullable) f.Property.SetValue(entity, null);
                        else errors.Add(f.Name, "اختر قيمة.");
                        return;
                    }
                    if (options.All(o => o.Value != raw))
                    {
                        errors.Add(f.Name, "اختر قيمة من القائمة.");
                        return;
                    }
                    f.Property.SetValue(entity, Convert(raw, f.Property.PropertyType));
                    return;

                case FieldKind.Media:
                    if (raw.Length == 0)
                    {
                        if (f.Required) errors.Add(f.Name, "اختر ملفًا من المكتبة.");
                        f.Property.SetValue(entity, null);
                        return;
                    }
                    var asset = long.TryParse(raw, out var id) ? await store.FindAsync<MediaAsset>(id, ct) : null;
                    if (asset is null || asset.Kind != f.MediaKind)
                    {
                        errors.Add(f.Name, f.MediaKind == MediaKind.Video ? "اختر فيديو من المكتبة." : "اختر صورة من المكتبة.");
                        return;
                    }
                    f.Property.SetValue(entity, asset.Id);
                    return;

                default:
                    CheckPlain(f, raw, errors);
                    f.Property.SetValue(entity, raw.Length == 0 && f.IsNullable ? null : raw);
                    return;
            }
        }

        /// <summary>The value of a non-text field as the form shows it.</summary>
        public static string Display(FieldDef f, object entity) => f.Property.GetValue(entity) switch
        {
            null => "",
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Enum e => System.Convert.ToInt32(e, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            IFormattable x => x.ToString(null, CultureInfo.InvariantCulture),
            var x => x.ToString() ?? ""
        };

        private static object? Convert(string value, Type type)
        {
            var target = Nullable.GetUnderlyingType(type) ?? type;
            if (target == typeof(string)) return value;
            if (target == typeof(long)) return long.Parse(value, CultureInfo.InvariantCulture);
            if (target == typeof(int)) return int.Parse(value, CultureInfo.InvariantCulture);
            if (target.IsEnum) return Enum.ToObject(target, int.Parse(value, CultureInfo.InvariantCulture));
            throw new NotSupportedException($"Select fields of type {type.Name} are not supported.");
        }

        /// <summary>Trims; single-line fields lose line breaks, others get "\n" line ends.</summary>
        private static string Clean(string? value, bool multiline)
        {
            var text = (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
            if (!multiline) text = text.Replace('\n', ' ');
            return text.Trim();
        }

        private static void CheckText(FieldDef f, string key, string value, string language, EditErrors errors)
        {
            if (f.Required && value.Length == 0) errors.Add(key, $"مطلوب {language}.");
            else if (f.MaxLength is { } max && value.Length > max) errors.Add(key, $"الحد الأقصى {max} حرفًا.");
        }

        private static void CheckPlain(FieldDef f, string value, EditErrors errors)
        {
            if (value.Length == 0)
            {
                if (f.Required) errors.Add(f.Name, "هذا الحقل مطلوب.");
                return;
            }
            if (f.MaxLength is { } max && value.Length > max)
            {
                errors.Add(f.Name, $"الحد الأقصى {max} حرفًا.");
                return;
            }
            var valid = f.Kind switch
            {
                FieldKind.Url => IsWebUrl(value),
                FieldKind.Email => IsEmail(value),
                FieldKind.Href => IsHref(value),
                _ => true
            };
            if (!valid)
                errors.Add(f.Name, f.Kind switch
                {
                    FieldKind.Url => "أدخل رابطًا كاملًا يبدأ بـ https://",
                    FieldKind.Email => "أدخل بريدًا إلكترونيًا صحيحًا.",
                    _ => "أدخل قسمًا من الصفحة مثل #consult، أو رابطًا يبدأ بـ https://، أو mailto: أو tel:."
                });
        }

        public static bool IsWebUrl(string value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            && !string.IsNullOrEmpty(uri.Host);

        public static bool IsEmail(string value) =>
            EmailPattern().IsMatch(value) && MailAddress.TryCreate(value, out var address) && address.Address == value;

        public static bool IsHref(string value) =>
            AnchorPattern().IsMatch(value) || IsWebUrl(value)
            || (value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) && IsEmail(value[7..].Split('?')[0]))
            || (value.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) && TelPattern().IsMatch(value[4..]));

        [GeneratedRegex(@"^[^@\s<>""']+@[^@\s<>""']+\.[^@\s<>""']+$")]
        private static partial Regex EmailPattern();

        [GeneratedRegex(@"^#[A-Za-z][A-Za-z0-9_-]*$")]
        private static partial Regex AnchorPattern();

        [GeneratedRegex(@"^\+?[0-9 ]{5,20}$")]
        private static partial Regex TelPattern();
    }
}
