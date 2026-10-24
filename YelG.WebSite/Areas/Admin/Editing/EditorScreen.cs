using Microsoft.EntityFrameworkCore.Metadata;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Domain.Common;

namespace YelG.WebSite.Areas.Admin.Editing
{
    /// <summary>A button on a screen that runs a task (e.g. build the marquee from the services); returns the message to show.</summary>
    public sealed record ScreenAction(string Slug, string Label, string Confirm, Func<IContentStore, CancellationToken, Task<string>> Run);

    /// <summary>One management screen: a section's own form (one row) and its lists.</summary>
    public sealed record EditorScreen
    {
        public required string Slug { get; init; }

        public required string Title { get; init; }

        public string? Description { get; init; }

        /// <summary>Where the preview opens ("#services").</summary>
        public string? Anchor { get; init; }

        /// <summary>Only Admins see and change it (site settings).</summary>
        public bool AdminOnly { get; init; }

        public EntityEditor? Main { get; init; }

        /// <summary>Loads the tracked row of <see cref="Main"/>; null until it is first saved.</summary>
        public Func<IContentStore, CancellationToken, Task<BaseEntity?>>? LoadMain { get; init; }

        public List<ListEditor> Lists { get; } = new();

        public List<ScreenAction> Actions { get; } = new();

        /// <summary>A list of this screen by its path ("services" or "services.deliverables").</summary>
        public ListEditor? FindList(string path)
        {
            var parts = path.Split('.');
            var list = Lists.FirstOrDefault(l => l.Slug == parts[0]);
            foreach (var part in parts.Skip(1)) list = list?.Children.FirstOrDefault(c => c.Slug == part);
            return list;
        }

        public IEnumerable<ListEditor> AllLists() => Lists.SelectMany(Flatten);

        private static IEnumerable<ListEditor> Flatten(ListEditor list) => new[] { list }.Concat(list.Children.SelectMany(Flatten));

        public static EditorScreen For<T>(string slug, string title, EntityEditor<T> main) where T : BaseEntity, new() => new()
        {
            Slug = slug,
            Title = title,
            Main = main,
            LoadMain = async (store, ct) => await store.FirstAsync<T>(ct)
        };
    }

    /// <summary>All screens, in menu order, and the names of the entity types for messages.</summary>
    public sealed class EditorRegistry
    {
        public EditorRegistry(IReadOnlyList<EditorScreen> screens) => Screens = screens;

        public IReadOnlyList<EditorScreen> Screens { get; }

        public EditorScreen? Find(string slug) => Screens.FirstOrDefault(s => s.Slug == slug);

        public IEnumerable<EntityEditor> Editors =>
            Screens.SelectMany(s => (s.Main is null ? [] : new[] { s.Main }).Concat(s.AllLists().Select(l => l.Item)));

        /// <summary>Tables without a screen that appear in the audit log.</summary>
        private static readonly Dictionary<string, string> OtherNames = new()
        {
            ["AdminUser"] = "حساب مستخدم",
            ["AdminRole"] = "دور",
            ["IdentityUserRole`1"] = "دور مستخدم",
            ["MediaAsset"] = "ملف وسائط",
            ["Enquiry"] = "طلب استشارة",
            ["EnquiryNote"] = "ملاحظة على طلب",
            ["Seed"] = "المحتوى الأولي"
        };

        /// <summary>"خدمة" for Service, "الواجهة الرئيسية" for HeroSection; the type name when unknown.</summary>
        public string NameOf(Type type) => NameOf(type.Name);

        /// <summary>The same by type name, as the audit log stores it.</summary>
        public string NameOf(string typeName) =>
            Editors.FirstOrDefault(e => e.EntityType.Name == typeName)?.Title ?? OtherNames.GetValueOrDefault(typeName) ?? typeName;

        /// <summary>The screen that edits a type, for links from the audit log and the media library.</summary>
        public EditorScreen? ScreenOf(Type type) =>
            Screens.FirstOrDefault(s => s.Main?.EntityType == type || s.AllLists().Any(l => l.Item.EntityType == type));

        /// <summary>Fills the maximum lengths that the database model sets.</summary>
        public void ApplyModel(IModel model)
        {
            foreach (var editor in Editors)
            {
                var entity = model.FindEntityType(editor.EntityType);
                if (entity is null) continue;
                foreach (var field in editor.Fields.Where(f => f.MaxLength is null))
                {
                    field.MaxLength = field.IsLocalized
                        ? entity.FindComplexProperty(field.Name)?.ComplexType.FindProperty(nameof(LocalizedText.Ar))?.GetMaxLength()
                        : field.Kind is FieldKind.Text or FieldKind.Url or FieldKind.Email or FieldKind.Href
                            ? entity.FindProperty(field.Name)?.GetMaxLength()
                            : null;
                }
            }
        }
    }
}
