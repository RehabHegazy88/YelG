using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace YelG.WebSite.Tests
{
    /// <summary>
    /// The seed must not invent or reword any copy: every text in seed-content.json (editable content) and in
    /// ui-strings.json (interface wording) has to appear, character for character, in the approved prototype
    /// of its language (docs/design/B-Ar.dc.html, B-En.dc.html).
    /// </summary>
    public class SeedContentFidelityTests
    {
        private static readonly string Root = FindRoot();

        private static string FindRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "YelG.AdminPortal.sln"))) dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("Solution folder not found.");
        }

        /// <summary>
        /// All text of a prototype, entities decoded, without whitespace: the visible text (tags removed) and
        /// the raw file, which also holds attribute texts (alt, aria-label) and the script's data.
        /// </summary>
        private static string Haystack(string file)
        {
            var raw = File.ReadAllText(Path.Combine(Root, "YelG.WebSite", "docs", "design", file));
            var visible = WebUtility.HtmlDecode(Regex.Replace(raw, "<[^>]+>", ""));
            var all = visible + "\n" + WebUtility.HtmlDecode(raw);
            return Regex.Replace(all.Replace("\\'", "'"), @"\s+", "");
        }

        /// <summary>A seeded text as it appears in the design: markup removed (an escaped \* is a real asterisk), without whitespace.</summary>
        private static string Needle(string value)
        {
            var text = Regex.Replace(value, @"\[([^\]]*)\]\([^)]*\)", "$1");   // [label](#anchor) -> label
            text = text.Replace("\\*", "\u0000").Replace("*", "").Replace("\u0000", "*");
            return Regex.Replace(text, @"\s+", "");
        }

        /// <summary>Every {"ar","en"} pair of the seed, with its JSON path.</summary>
        private static IEnumerable<(string Path, string Ar, string En)> Pairs(JsonElement node, string path)
        {
            if (node.ValueKind == JsonValueKind.Object)
            {
                // {"ar","en"}, or a menu entry {"href","ar","en"}
                if (node.TryGetProperty("ar", out var ar) && node.TryGetProperty("en", out var en)
                    && ar.ValueKind == JsonValueKind.String && en.ValueKind == JsonValueKind.String)
                {
                    yield return (path, ar.GetString()!, en.GetString()!);
                    yield break;
                }
                foreach (var p in node.EnumerateObject())
                    foreach (var x in Pairs(p.Value, $"{path}.{p.Name}"))
                        yield return x;
            }
            else if (node.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var item in node.EnumerateArray())
                    foreach (var x in Pairs(item, $"{path}[{i++}]"))
                        yield return x;
            }
        }

        [Fact]
        public void Every_seeded_text_appears_in_the_prototype_of_its_language() =>
            AssertAllInPrototypes(Path.Combine("YelG.WebSite.Infrastructure", "Seeding", "Data", "seed-content.json"), minimum: 400);

        [Fact]
        public void Every_interface_text_appears_in_the_prototype_of_its_language() =>
            AssertAllInPrototypes(Path.Combine("YelG.WebSite", "Resources", "ui-strings.json"), minimum: 60);

        private static void AssertAllInPrototypes(string relativePath, int minimum)
        {
            var json = File.ReadAllText(Path.Combine(Root, relativePath));
            using var doc = JsonDocument.Parse(json);
            var arabic = Haystack("B-Ar.dc.html");
            var english = Haystack("B-En.dc.html");

            var missing = new List<string>();
            var checkedCount = 0;
            foreach (var (path, ar, en) in Pairs(doc.RootElement, "$"))
            {
                // Placeholders are filled when rendering: the copyright year, the prefill template, counters.
                foreach (var (lang, value, haystack) in new[] { ("ar", ar, arabic), ("en", en, english) })
                {
                    if (string.IsNullOrWhiteSpace(value)) continue;
                    var text = value.Replace("{year}", "2026");
                    var parts = text.Split(new[] { "{score}", "{band}", "{n}", "{total}", "{max}", "{count}" }, StringSplitOptions.None);
                    foreach (var part in parts.Where(x => !string.IsNullOrWhiteSpace(x)))
                    {
                        checkedCount++;
                        if (!haystack.Contains(Needle(part), StringComparison.Ordinal))
                            missing.Add($"{path} [{lang}]: {part}");
                    }
                }
            }

            Assert.True(checkedCount > minimum, $"Only {checkedCount} texts checked in {relativePath}; it looks incomplete.");
            Assert.True(missing.Count == 0, "Texts not found in the prototypes:\n" + string.Join("\n", missing));
        }

        [Fact]
        public void Seeded_media_files_exist_in_wwwroot_assets()
        {
            var json = File.ReadAllText(Path.Combine(Root, "YelG.WebSite.Infrastructure", "Seeding", "Data", "seed-content.json"));
            using var doc = JsonDocument.Parse(json);
            var files = doc.RootElement.GetProperty("media").EnumerateArray().Select(x => x.GetString()!).ToList();

            Assert.Equal(11, files.Count);
            Assert.All(files, f => Assert.True(File.Exists(Path.Combine(Root, "YelG.WebSite", "wwwroot", "assets", f)), f));
        }
    }
}
