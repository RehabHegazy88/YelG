using System.Reflection;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Media;

namespace YelG.WebSite.Areas.Admin.Editing
{
    public enum FieldKind
    {
        /// <summary>One line; LocalizedText (Ar/En side by side) or a plain string.</summary>
        Text,

        /// <summary>Several lines; LocalizedText.</summary>
        TextArea,

        /// <summary>An https address.</summary>
        Url,

        Email,

        /// <summary>A link target: an anchor on the page ("#consult"), an https address, mailto: or tel:.</summary>
        Href,

        Bool,
        Int,
        Date,

        /// <summary>A choice from a list: a key (string), an enum, or another row (its id).</summary>
        Select,

        /// <summary>An image or video from the media library (its id).</summary>
        Media
    }

    public sealed record FieldOption(string Value, string Label);

    /// <summary>One editable property of an entity, as the admin form shows and reads it.</summary>
    public sealed class FieldDef
    {
        public required PropertyInfo Property { get; init; }

        public required FieldKind Kind { get; init; }

        public required string Label { get; init; }

        /// <summary>The property name, which is also the form field name ("Heading" → "Heading.Ar" / "Heading.En").</summary>
        public string Name => Property.Name;

        public string? Hint { get; init; }

        /// <summary>For text: both languages must be filled.</summary>
        public bool Required { get; init; }

        /// <summary>Taken from the database model when not given.</summary>
        public int? MaxLength { get; set; }

        /// <summary>The text may use the site markup (*highlight*, a new line, [label](#anchor)).</summary>
        public bool Markup { get; init; }

        /// <summary>Plain strings written left to right (keys, codes, addresses).</summary>
        public bool Ltr { get; init; }

        public int Rows { get; init; } = 3;

        public int? Min { get; init; }
        public int? Max { get; init; }

        public MediaKind? MediaKind { get; init; }

        /// <summary>Fixed choices, or choices read from the database when the form is shown.</summary>
        public IReadOnlyList<FieldOption>? FixedOptions { get; init; }
        public Func<IContentStore, CancellationToken, Task<IReadOnlyList<FieldOption>>>? LoadOptions { get; init; }

        /// <summary>A select or media field that may be left empty; the label of the empty choice.</summary>
        public string? EmptyLabel { get; init; }

        /// <summary>Heading of the group of fields this one starts (a fieldset legend).</summary>
        public string? Group { get; init; }

        public bool IsLocalized => Property.PropertyType == typeof(LocalizedText);

        public bool IsNullable =>
            Nullable.GetUnderlyingType(Property.PropertyType) is not null
            || (!Property.PropertyType.IsValueType && new NullabilityInfoContext().Create(Property).WriteState == NullabilityState.Nullable);

        public async Task<IReadOnlyList<FieldOption>> OptionsAsync(IContentStore store, CancellationToken ct) =>
            FixedOptions ?? (LoadOptions is null ? [] : await LoadOptions(store, ct));
    }
}
