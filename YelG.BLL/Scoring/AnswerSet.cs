namespace YelG.BLL.Scoring
{
    /// <summary>An evaluation's answers keyed by question code (the old company.answers object).</summary>
    public sealed class AnswerSet
    {
        private static readonly IReadOnlyList<string> Empty = Array.Empty<string>();
        private readonly Dictionary<string, IReadOnlyList<string>> _values;

        public AnswerSet(IDictionary<string, IReadOnlyList<string>>? values = null)
        {
            _values = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            if (values is null) return;
            foreach (var pair in values)
                _values[pair.Key] = Clean(pair.Value);
        }

        public static AnswerSet From(IEnumerable<KeyValuePair<string, IReadOnlyList<string>>> values) =>
            new(values.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal));

        /// <summary>Trimmed, non-empty values for a question; empty when unanswered.</summary>
        public IReadOnlyList<string> ValuesOf(string questionCode) =>
            _values.TryGetValue(questionCode, out var list) ? list : Empty;

        public bool IsAnswered(string questionCode) => ValuesOf(questionCode).Count > 0;

        public IReadOnlyDictionary<string, IReadOnlyList<string>> ToDictionary() => _values;

        private static IReadOnlyList<string> Clean(IEnumerable<string?>? values) =>
            values?.Where(v => v is not null)
                   .Select(v => v!.Trim())
                   .Where(v => v.Length > 0)
                   .ToArray()
            ?? Empty;
    }
}
