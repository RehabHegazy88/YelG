using System.Linq.Expressions;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Domain.Common;

namespace YelG.WebSite.Areas.Admin.Editing
{
    /// <summary>
    /// A column of a list besides the item's name. <see cref="Prepare"/> runs once per list (it may load a
    /// lookup, e.g. category names) and returns the value of a row.
    /// </summary>
    public sealed record ListColumn(string Header, Func<IContentStore, CancellationToken, Task<Func<ContentItem, string>>> Prepare);

    /// <summary>
    /// An ordered list of items on a screen (hero stats, services, …): list, add, edit, delete, reorder and
    /// show / hide. A list can belong to an item of another list (a service's deliverables).
    /// </summary>
    public abstract class ListEditor
    {
        protected ListEditor(string slug, string title, EntityEditor item)
        {
            Slug = slug;
            Title = title;
            Item = item;
        }

        public string Slug { get; }

        /// <summary>Plural ("الخدمات").</summary>
        public string Title { get; }

        public EntityEditor Item { get; }

        public string? Description { get; init; }

        public bool CanAdd { get; init; } = true;

        public bool CanDelete { get; init; } = true;

        public List<ListColumn> Columns { get; } = new();

        public ListEditor? Parent { get; private set; }

        public List<ListEditor> Children { get; } = new();

        /// <summary>"services" or, for a child list, "services.deliverables".</summary>
        public string Path => Parent is null ? Slug : $"{Parent.Path}.{Slug}";

        public ListEditor Child(ListEditor child)
        {
            child.Parent = this;
            Children.Add(child);
            return this;
        }

        /// <summary>The items in order (all of them for a top-level list, those of one parent for a child list).</summary>
        public abstract Task<List<ContentItem>> ListAsync(IContentStore store, long? parentId, CancellationToken ct);

        /// <summary>Tracked, for renumbering.</summary>
        public abstract Task<List<ContentItem>> ListForUpdateAsync(IContentStore store, long? parentId, CancellationToken ct);

        public abstract Task<ContentItem?> FindAsync(IContentStore store, long id, CancellationToken ct);

        public abstract ContentItem Create(long? parentId);

        /// <summary>The parent item's id for a child list; null for a top-level list.</summary>
        public abstract long? ParentIdOf(ContentItem item);
    }

    public sealed class ListEditor<T> : ListEditor where T : ContentItem, new()
    {
        private readonly Func<T, long>? _getParent;
        private readonly Action<T, long>? _setParent;
        private readonly string? _parentProperty;

        public ListEditor(string slug, string title, EntityEditor<T> item, Expression<Func<T, long>>? parentKey = null)
            : base(slug, title, item)
        {
            if (parentKey is null) return;
            var member = (MemberExpression)parentKey.Body;
            _parentProperty = member.Member.Name;
            _getParent = parentKey.Compile();
            var property = typeof(T).GetProperty(_parentProperty)!;
            _setParent = (item, id) => property.SetValue(item, id);
        }

        /// <summary>Adds a column.</summary>
        public ListEditor<T> Show(string header, Func<T, string> value)
        {
            Columns.Add(new ListColumn(header, (_, _) => Task.FromResult<Func<ContentItem, string>>(i => value((T)i))));
            return this;
        }

        /// <summary>Adds a column whose values need a lookup, loaded once per list.</summary>
        public ListEditor<T> Show<TRef>(string header, Func<T, long?> key, Func<TRef, string> name) where TRef : ContentItem
        {
            Columns.Add(new ListColumn(header, async (store, ct) =>
            {
                var names = (await store.ListAsync<TRef>(ct: ct)).ToDictionary(r => r.Id, name);
                return i => key((T)i) is { } id && names.TryGetValue(id, out var n) ? n : "—";
            }));
            return this;
        }

        public new ListEditor<T> Child(ListEditor child)
        {
            base.Child(child);
            return this;
        }

        public override Task<List<ContentItem>> ListAsync(IContentStore store, long? parentId, CancellationToken ct) =>
            Cast(store.ListAsync(Filter(parentId), ct));

        public override Task<List<ContentItem>> ListForUpdateAsync(IContentStore store, long? parentId, CancellationToken ct) =>
            Cast(store.ListForUpdateAsync(Filter(parentId), ct));

        public override async Task<ContentItem?> FindAsync(IContentStore store, long id, CancellationToken ct) =>
            await store.FindAsync<T>(id, ct);

        public override ContentItem Create(long? parentId)
        {
            var item = new T();
            if (_setParent is not null) _setParent(item, parentId ?? throw new ArgumentNullException(nameof(parentId)));
            return item;
        }

        public override long? ParentIdOf(ContentItem item) => _getParent?.Invoke((T)item);

        private Expression<Func<T, bool>>? Filter(long? parentId)
        {
            if (_parentProperty is null) return null;
            var x = Expression.Parameter(typeof(T), "x");
            var holder = new ParentHolder(parentId ?? 0);
            var body = Expression.Equal(Expression.Property(x, _parentProperty), Expression.Property(Expression.Constant(holder), nameof(ParentHolder.Id)));
            return Expression.Lambda<Func<T, bool>>(body, x);
        }

        private static async Task<List<ContentItem>> Cast(Task<List<T>> items) => (await items).Cast<ContentItem>().ToList();

        /// <summary>Lets EF send the parent id as a parameter.</summary>
        private sealed class ParentHolder(long id)
        {
            public long Id { get; } = id;
        }
    }
}
