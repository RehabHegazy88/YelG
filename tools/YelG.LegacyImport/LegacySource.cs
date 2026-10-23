using System.Text.Json;
using ClosedXML.Excel;
using YelG.BLL.Common;

namespace YelG.LegacyImport
{
    public sealed record LegacyUser(string Key, string UserName, string DisplayName, bool IsAdmin, bool IsActive, DateTimeOffset CreatedAt);

    public sealed record LegacyCompany(string Id, string Name, string Activity, DateTimeOffset CreatedAt);

    public sealed record LegacyEvaluation(
        string Id, string CompanyId, string CompanyName, string Activity, string OwnerKey, string Status,
        string? AssignedBy, DateTimeOffset? AssignedAt, DateTimeOffset? EditedAt,
        IReadOnlyDictionary<string, IReadOnlyList<string>> Answers, string? Judge, string? Note);

    public sealed record LegacyLogRow(DateTimeOffset At, string Key, string UserName, string Action, string Details);

    /// <summary>
    /// The old Apps Script data: "YELG Lens Database.xlsx" (Users, Companies, ActivityLog sheets, times in
    /// Cairo local time) and one state-*.json per user (each user's evaluations; times in UTC).
    /// PIN hashes in the Users sheet are never read.
    /// </summary>
    public sealed class LegacySource
    {
        public required List<LegacyUser> Users { get; init; }
        public required List<LegacyCompany> Companies { get; init; }
        public required List<LegacyEvaluation> Evaluations { get; init; }
        public required List<LegacyLogRow> Log { get; init; }

        public static LegacySource Load(string folder)
        {
            using var book = new XLWorkbook(Path.Combine(folder, "YELG Lens Database.xlsx"));
            return new LegacySource
            {
                Users = Rows(book, "Users").Select(r => new LegacyUser(
                    Key(r["Key"]), r["Username"].GetString().Trim(), r["DisplayName"].GetString().Trim(),
                    r["Role"].GetString().Trim().Equals("admin", StringComparison.OrdinalIgnoreCase),
                    Bool(r["Active"]), Cairo(r["CreatedAt"]) ?? DateTimeOffset.UtcNow)).ToList(),
                Companies = Rows(book, "Companies").Select(r => new LegacyCompany(
                    r["CompanyId"].GetString().Trim(), r["Name"].GetString(), r["Activity"].GetString().Trim(),
                    Cairo(r["CreatedAt"]) ?? DateTimeOffset.UtcNow)).ToList(),
                Log = Rows(book, "ActivityLog").Select(r => new LegacyLogRow(
                    Cairo(r["Time"]) ?? DateTimeOffset.MinValue, Key(r["Key"]), r["Username"].GetString().Trim(),
                    r["Action"].GetString().Trim(), r["Details"].GetString())).ToList(),
                Evaluations = Directory.GetFiles(folder, "state-*.json").OrderBy(f => f).SelectMany(ReadState).ToList()
            };
        }

        public static string Key(IXLCell cell) => Key(cell.GetString());

        /// <summary>Same as userKey_ in the Apps Script: trimmed, spaces collapsed, lower case.</summary>
        public static string Key(string? value) => Text.Clean(value, 80).ToLowerInvariant();

        private static IEnumerable<Dictionary<string, IXLCell>> Rows(XLWorkbook book, string sheet)
        {
            var ws = book.Worksheet(sheet);
            var headers = ws.Row(1).CellsUsed().ToDictionary(c => c.GetString().Trim(), c => c.Address.ColumnNumber);
            foreach (var row in ws.RowsUsed().Skip(1))
                yield return headers.ToDictionary(h => h.Key, h => row.Cell(h.Value));
        }

        private static bool Bool(IXLCell cell) =>
            cell.DataType == XLDataType.Boolean ? cell.GetBoolean()
            : cell.GetString().Trim().ToLowerInvariant() is "true" or "1" or "yes";

        private static DateTimeOffset? Cairo(IXLCell cell)
        {
            if (cell.IsEmpty()) return null;
            if (cell.DataType == XLDataType.DateTime) return Labels.FromCairo(cell.GetDateTime());
            return DateTime.TryParse(cell.GetString(), System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsed) ? Labels.FromCairo(parsed) : null;
        }

        private static IEnumerable<LegacyEvaluation> ReadState(string path)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            // state-<key>.json; "_audit.ownerKey" has the real key (the file name replaces spaces with "_").
            var fileOwner = root.TryGetProperty("_audit", out var audit) && audit.TryGetProperty("ownerKey", out var ok)
                ? Key(ok.GetString())
                : Key(Path.GetFileNameWithoutExtension(path)["state-".Length..].Replace('_', ' '));
            DateTimeOffset? editedAt = audit.ValueKind == JsonValueKind.Object && audit.TryGetProperty("lastEditedAt", out var le)
                && DateTimeOffset.TryParse(le.GetString(), out var t) ? t : null;
            if (!root.TryGetProperty("COMPANIES", out var companies)) yield break;

            foreach (var c in companies.EnumerateArray())
            {
                var answers = new Dictionary<string, IReadOnlyList<string>>();
                if (c.TryGetProperty("answers", out var a))
                    foreach (var p in a.EnumerateObject())
                    {
                        var values = (p.Value.ValueKind == JsonValueKind.Array ? p.Value.EnumerateArray().ToList() : new() { p.Value })
                            .Select(v => v.ValueKind == JsonValueKind.String ? v.GetString()! : v.ToString())
                            .Select(v => v.Trim()).Where(v => v.Length > 0).ToList();
                        if (values.Count > 0) answers[p.Name] = values;
                    }

                yield return new LegacyEvaluation(
                    Str(c, "id")!, Str(c, "companyId") ?? Str(c, "id")!, Str(c, "name") ?? "", Str(c, "activity") ?? "",
                    Str(c, "ownerKey") is { } owner ? Key(owner) : fileOwner, Str(c, "status") ?? "draft",
                    Str(c, "assignedBy") is { } by ? Key(by) : null,
                    DateTimeOffset.TryParse(Str(c, "assignedAt"), out var at) ? at : null,
                    editedAt, answers, Str(c, "judge"), Str(c, "note"));
            }
        }

        private static string? Str(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString())
                ? v.GetString() : null;
    }
}
