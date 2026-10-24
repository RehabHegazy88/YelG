using YelG.WebSite.Areas.Admin.Editing;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Media;

namespace YelG.WebSite.Areas.Admin.Models
{
    /// <summary>An entity's form: its fields with the current (or just posted) values.</summary>
    public sealed class EditorFormModel
    {
        public required EntityEditor Editor { get; init; }
        public required BaseEntity Entity { get; init; }
        public required string PostUrl { get; init; }
        public bool IsNew { get; init; }
        public EditErrors Errors { get; init; } = new();
        public IReadOnlyDictionary<string, IReadOnlyList<FieldOption>> Options { get; init; } = new Dictionary<string, IReadOnlyList<FieldOption>>();
        public IReadOnlyDictionary<long, MediaAsset> Media { get; init; } = new Dictionary<long, MediaAsset>();
        public string? CancelUrl { get; init; }

        /// <summary>The row's last change when the form was opened, to detect someone else's save in between.</summary>
        public long Stamp => Entity.LastModifiedDate.Ticks;
    }

    public sealed class ListModel
    {
        public required EditorScreen Screen { get; init; }
        public required ListEditor List { get; init; }
        public long? ParentId { get; init; }
        public required IReadOnlyList<ContentItem> Items { get; init; }
        public required IReadOnlyList<(string Header, Func<ContentItem, string> Value)> Columns { get; init; }

        public string BaseUrl => $"/admin/sections/{Screen.Slug}/{List.Path}";
        public string NewUrl => ParentId is null ? $"{BaseUrl}/new" : $"{BaseUrl}/new?parent={ParentId}";
        public string Anchor => "list-" + List.Path.Replace('.', '-');
    }

    public sealed record Crumb(string Title, string? Url);

    public sealed class ScreenPageModel
    {
        public required EditorScreen Screen { get; init; }
        public EditorFormModel? Form { get; init; }
        public required IReadOnlyList<ListModel> Lists { get; init; }
    }

    public sealed class ItemPageModel
    {
        public required EditorScreen Screen { get; init; }
        public required ListEditor List { get; init; }
        public required EditorFormModel Form { get; init; }
        public required IReadOnlyList<ListModel> Children { get; init; }
        public required IReadOnlyList<Crumb> Crumbs { get; init; }
        public required string Title { get; init; }
    }
}
