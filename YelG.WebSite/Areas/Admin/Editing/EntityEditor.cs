using System.Linq.Expressions;
using System.Reflection;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Media;

namespace YelG.WebSite.Areas.Admin.Editing
{
    /// <summary>What a save is checked against, besides the field rules.</summary>
    public sealed class EditContext
    {
        public EditContext(IContentStore store, EditErrors errors, bool isNew, CancellationToken ct)
        {
            Store = store;
            Errors = errors;
            IsNew = isNew;
            CancellationToken = ct;
        }

        public IContentStore Store { get; }
        public EditErrors Errors { get; }
        public bool IsNew { get; }
        public CancellationToken CancellationToken { get; }
    }

    /// <summary>The form of one entity type: its fields in order, extra checks and how a row is named in lists.</summary>
    public abstract class EntityEditor
    {
        protected EntityEditor(string title) => Title = title;

        /// <summary>Singular name ("خدمة"), or the section name for a section's own form.</summary>
        public string Title { get; }

        public abstract Type EntityType { get; }

        public List<FieldDef> Fields { get; } = new();

        public FieldDef? Field(string name) => Fields.FirstOrDefault(f => f.Name == name);

        public abstract Task ValidateAsync(BaseEntity entity, EditContext context);

        /// <summary>How the row is named in its list and in messages.</summary>
        public abstract string Summary(BaseEntity entity);

        public abstract BaseEntity New();
    }

    public sealed class EntityEditor<T> : EntityEditor where T : BaseEntity, new()
    {
        private readonly List<Func<T, EditContext, Task>> _rules = new();
        private Func<T, string>? _summary;
        private string? _group;

        public EntityEditor(string title) : base(title)
        {
        }

        public override Type EntityType => typeof(T);

        public override BaseEntity New() => new T();

        // ---------- fields ----------

        /// <summary>Starts a group of fields under a heading.</summary>
        public EntityEditor<T> Group(string heading)
        {
            _group = heading;
            return this;
        }

        public EntityEditor<T> Text(Expression<Func<T, LocalizedText>> property, string label, bool required = false,
            string? hint = null, bool markup = false, int? max = null) =>
            Add(property, FieldKind.Text, label, f => new FieldDef
            {
                Property = f, Kind = FieldKind.Text, Label = label, Required = required, Hint = hint, Markup = markup, MaxLength = max, Group = TakeGroup()
            });

        public EntityEditor<T> Area(Expression<Func<T, LocalizedText>> property, string label, bool required = false,
            string? hint = null, bool markup = false, int rows = 3) =>
            Add(property, FieldKind.TextArea, label, f => new FieldDef
            {
                Property = f, Kind = FieldKind.TextArea, Label = label, Required = required, Hint = hint, Markup = markup, Rows = rows, Group = TakeGroup()
            });

        /// <summary>A plain string in one language (a key, a code, a Latin text).</summary>
        public EntityEditor<T> Plain(Expression<Func<T, string?>> property, string label, bool required = false,
            string? hint = null, bool ltr = true, int? max = null) =>
            Add(property, FieldKind.Text, label, f => new FieldDef
            {
                Property = f, Kind = FieldKind.Text, Label = label, Required = required, Hint = hint, Ltr = ltr, MaxLength = max, Group = TakeGroup()
            });

        public EntityEditor<T> Url(Expression<Func<T, string?>> property, string label, bool required = false, string? hint = null) =>
            Typed(property, FieldKind.Url, label, required, hint);

        public EntityEditor<T> Email(Expression<Func<T, string?>> property, string label, bool required = false, string? hint = null) =>
            Typed(property, FieldKind.Email, label, required, hint);

        public EntityEditor<T> Href(Expression<Func<T, string?>> property, string label, bool required = false, string? hint = null) =>
            Typed(property, FieldKind.Href, label, required, hint ?? "قسم من الصفحة مثل #consult، أو رابط https://، أو mailto: أو tel:.");

        public EntityEditor<T> Bool(Expression<Func<T, bool>> property, string label, string? hint = null) =>
            Add(property, FieldKind.Bool, label, f => new FieldDef { Property = f, Kind = FieldKind.Bool, Label = label, Hint = hint, Group = TakeGroup() });

        public EntityEditor<T> Int(Expression<Func<T, int>> property, string label, int min, int max, string? hint = null) =>
            Add(property, FieldKind.Int, label, f => new FieldDef
            {
                Property = f, Kind = FieldKind.Int, Label = label, Min = min, Max = max, Hint = hint, Group = TakeGroup()
            });

        public EntityEditor<T> Date(Expression<Func<T, DateOnly>> property, string label, string? hint = null) =>
            Add(property, FieldKind.Date, label, f => new FieldDef { Property = f, Kind = FieldKind.Date, Label = label, Hint = hint, Group = TakeGroup() });

        /// <summary>A date that may be left empty.</summary>
        public EntityEditor<T> OptionalDate(Expression<Func<T, DateOnly?>> property, string label, string? hint = null) =>
            Add(property, FieldKind.Date, label, f => new FieldDef { Property = f, Kind = FieldKind.Date, Label = label, Hint = hint, Group = TakeGroup() });

        /// <summary>A key from a fixed list (an icon, a colour, a language).</summary>
        public EntityEditor<T> Choice(Expression<Func<T, string>> property, string label, IEnumerable<FieldOption> options, string? hint = null) =>
            Add(property, FieldKind.Select, label, f => new FieldDef
            {
                Property = f, Kind = FieldKind.Select, Label = label, FixedOptions = options.ToList(), Hint = hint, Group = TakeGroup()
            });

        public EntityEditor<T> Choice<TEnum>(Expression<Func<T, TEnum>> property, string label, IReadOnlyDictionary<TEnum, string> labels,
            string? hint = null) where TEnum : struct, Enum =>
            Add(property, FieldKind.Select, label, f => new FieldDef
            {
                Property = f, Kind = FieldKind.Select, Label = label, Hint = hint, Group = TakeGroup(),
                FixedOptions = labels.Select(l => new FieldOption(Convert.ToInt32(l.Key).ToString(), l.Value)).ToList()
            });

        /// <summary>Another row (a category, a service), listed by its Arabic name.</summary>
        public EntityEditor<T> Reference<TRef>(Expression<Func<T, long?>> property, string label, Func<TRef, string> name,
            string? emptyLabel = null, string? hint = null) where TRef : ContentItem =>
            Add(property, FieldKind.Select, label, f => new FieldDef
            {
                Property = f, Kind = FieldKind.Select, Label = label, Hint = hint, EmptyLabel = emptyLabel, Group = TakeGroup(),
                LoadOptions = async (store, ct) => (await store.ListAsync<TRef>(ct: ct)).Select(r => new FieldOption(r.Id.ToString(), name(r))).ToList()
            });

        public EntityEditor<T> Media(Expression<Func<T, long?>> property, string label, MediaKind kind = MediaKind.Image,
            bool required = false, string? hint = null) =>
            Add(property, FieldKind.Media, label, f => new FieldDef
            {
                Property = f, Kind = FieldKind.Media, Label = label, MediaKind = kind, Required = required, Hint = hint,
                EmptyLabel = required ? null : "بدون", Group = TakeGroup()
            });

        // ---------- rules and naming ----------

        /// <summary>An extra check run on save, after the field rules.</summary>
        public EntityEditor<T> Rule(Func<T, EditContext, Task> rule)
        {
            _rules.Add(rule);
            return this;
        }

        public EntityEditor<T> Rule(Action<T, EditContext> rule) => Rule((e, c) =>
        {
            rule(e, c);
            return Task.CompletedTask;
        });

        /// <summary>The value of <paramref name="property"/> must not be used by another row (keys).</summary>
        public EntityEditor<T> Unique(Expression<Func<T, string>> property, string message)
        {
            var name = PropertyOf(property).Name;
            var get = property.Compile();
            return Rule(async (entity, context) =>
            {
                var value = get(entity);
                var parameter = Expression.Parameter(typeof(T), "x");
                var holder = new ValueHolder(value, entity.Id);
                var sameValue = Expression.Equal(Expression.Property(parameter, name), Expression.Property(Expression.Constant(holder), nameof(ValueHolder.Value)));
                var otherRow = Expression.NotEqual(Expression.Property(parameter, nameof(BaseEntity.Id)), Expression.Property(Expression.Constant(holder), nameof(ValueHolder.Id)));
                var filter = Expression.Lambda<Func<T, bool>>(Expression.AndAlso(sameValue, otherRow), parameter);
                if (await context.Store.AnyAsync(filter, context.CancellationToken)) context.Errors.Add(name, message);
            });
        }

        public EntityEditor<T> SummaryOf(Func<T, string> summary)
        {
            _summary = summary;
            return this;
        }

        public override async Task ValidateAsync(BaseEntity entity, EditContext context)
        {
            foreach (var rule in _rules) await rule((T)entity, context);
        }

        public override string Summary(BaseEntity entity)
        {
            if (_summary is not null) return _summary((T)entity);
            var first = Fields.FirstOrDefault(f => f.IsLocalized);
            var text = first is null ? null : ((LocalizedText)first.Property.GetValue(entity)!).Ar;
            return string.IsNullOrWhiteSpace(text) ? $"{Title} {entity.Id}" : text;
        }

        // ---------- helpers ----------

        private sealed class ValueHolder(string value, long id)
        {
            public string Value { get; } = value;
            public long Id { get; } = id;
        }

        private string? TakeGroup()
        {
            var group = _group;
            _group = null;
            return group;
        }

        private EntityEditor<T> Typed(Expression<Func<T, string?>> property, FieldKind kind, string label, bool required, string? hint) =>
            Add(property, kind, label, f => new FieldDef
            {
                Property = f, Kind = kind, Label = label, Required = required, Hint = hint, Ltr = true, Group = TakeGroup()
            });

        private EntityEditor<T> Add<TProp>(Expression<Func<T, TProp>> property, FieldKind kind, string label, Func<PropertyInfo, FieldDef> create)
        {
            var info = PropertyOf(property);
            if (Fields.Any(f => f.Name == info.Name)) throw new InvalidOperationException($"{typeof(T).Name}.{info.Name} is added twice.");
            Fields.Add(create(info));
            return this;
        }

        private static PropertyInfo PropertyOf<TProp>(Expression<Func<T, TProp>> property)
        {
            var body = property.Body is UnaryExpression { NodeType: ExpressionType.Convert } convert ? convert.Operand : property.Body;
            return body is MemberExpression { Member: PropertyInfo info }
                ? info
                : throw new ArgumentException("Expected a property, e.g. x => x.Heading.", nameof(property));
        }
    }
}
