using System.Text.Json;

namespace YelG.WebSite.Localization
{
    /// <summary>One entry of a menu: the in-page anchor and its label in both languages.</summary>
    public sealed record UiLink(string Href, string Ar, string En)
    {
        public string For(string lang) => lang == SiteLanguage.English ? En : Ar;
    }

    /// <summary>
    /// The fixed interface wording of the public site (menus, screen-reader labels, form labels, validation
    /// messages). Generated from the approved prototypes by tools/website/extract-seed.py into
    /// Resources/ui-strings.json and embedded; it is not editable content.
    /// </summary>
    public sealed class UiStrings
    {
        private const string ResourceName = "YelG.WebSite.Resources.ui-strings.json";

        private readonly Dictionary<string, (string Ar, string En)> _texts = new();
        private readonly Dictionary<string, IReadOnlyList<UiLink>> _links = new();

        private UiStrings()
        {
        }

        public static UiStrings Load()
        {
            using var stream = typeof(UiStrings).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Embedded resource {ResourceName} is missing.");
            using var json = JsonDocument.Parse(stream);
            var strings = new UiStrings();
            foreach (var property in json.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                    strings._links[property.Name] = property.Value.EnumerateArray()
                        .Select(l => new UiLink(l.GetProperty("href").GetString()!, l.GetProperty("ar").GetString()!, l.GetProperty("en").GetString()!))
                        .ToList();
                else
                    strings._texts[property.Name] = (property.Value.GetProperty("ar").GetString()!, property.Value.GetProperty("en").GetString()!);
            }
            return strings;
        }

        /// <summary>The text for a key in a language; throws for an unknown key so a typo fails loudly.</summary>
        public string Get(string key, string lang) =>
            _texts.TryGetValue(key, out var t) ? (lang == SiteLanguage.English ? t.En : t.Ar)
                : throw new KeyNotFoundException($"UI string '{key}' is not in ui-strings.json.");

        public IReadOnlyList<UiLink> Links(string key) =>
            _links.TryGetValue(key, out var l) ? l : throw new KeyNotFoundException($"UI list '{key}' is not in ui-strings.json.");

        /// <summary>The validation messages of the consultation form, keyed by field ("name", "email", …).</summary>
        public IReadOnlyDictionary<string, string> Validation(string lang) =>
            _texts.Where(t => t.Key.StartsWith("validation.", StringComparison.Ordinal))
                  .ToDictionary(t => t.Key["validation.".Length..], t => lang == SiteLanguage.English ? t.Value.En : t.Value.Ar);
    }
}
