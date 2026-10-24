using Microsoft.AspNetCore.Mvc;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Application.Common;
using YelG.WebSite.Areas.Admin.Editing;
using YelG.WebSite.Areas.Admin.Models;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Media;

namespace YelG.WebSite.Areas.Admin.Controllers
{
    /// <summary>
    /// The management screens of the homepage sections, all driven by <see cref="AdminScreens"/>: a section's
    /// own form, and for each of its lists add, edit, delete, reorder and show / hide.
    /// </summary>
    [Route("admin/sections/{screen}")]
    public sealed class SectionsController : AdminController
    {
        private const string StampField = "_stamp";
        private const string ConcurrencyMessage =
            "عدّل مستخدم آخر هذا المحتوى بعد أن فتحت الصفحة، فلم يُحفظ تعديلك. انسخ ما كتبته، ثم أعد فتح الصفحة.";

        private readonly EditorRegistry _registry;
        private readonly IContentStore _store;

        public SectionsController(EditorRegistry registry, IContentStore store)
        {
            _registry = registry;
            _store = store;
        }

        // ------------------------------------------------------------ the section form

        [HttpGet("")]
        public async Task<IActionResult> Index(string screen, CancellationToken ct)
        {
            if (Screen(screen) is not { } s) return NotFound();
            var entity = s.LoadMain is null ? null : await s.LoadMain(_store, ct) ?? s.Main!.New();
            return View("Screen", await ScreenPage(s, entity, new EditErrors(), ct));
        }

        [HttpPost("")]
        public async Task<IActionResult> Save(string screen, CancellationToken ct)
        {
            if (Screen(screen) is not { Main: { } editor, LoadMain: { } load } s) return NotFound();
            var entity = await load(_store, ct);
            var isNew = entity is null;
            entity ??= editor.New();

            var errors = await BindAsync(editor, entity, isNew, ct);
            if (errors.Any)
            {
                Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                return View("Screen", await ScreenPage(s, entity, errors, ct));
            }

            if (isNew) _store.Add(entity);
            await _store.SaveChangesAsync(ct);
            Flash("تم حفظ التعديلات.");
            return Redirect(ScreenUrl(s));
        }

        [HttpPost("run/{task}")]
        public async Task<IActionResult> Run(string screen, string task, CancellationToken ct)
        {
            if (Screen(screen) is not { } s || s.Actions.FirstOrDefault(a => a.Slug == task) is not { } run) return NotFound();
            Flash(await run.Run(_store, ct));
            return Redirect(ScreenUrl(s));
        }

        // ------------------------------------------------------------ list items

        [HttpGet("{list}/new")]
        public async Task<IActionResult> New(string screen, string list, long? parent, CancellationToken ct)
        {
            if (await Resolve(screen, list, parent, ct) is not (var s, var l, _)) return NotFound();
            var item = l.Create(parent);
            return View("Item", await ItemPage(s, l, item, isNew: true, new EditErrors(), ct));
        }

        [HttpPost("{list}/new")]
        public async Task<IActionResult> Create(string screen, string list, long? parent, CancellationToken ct)
        {
            if (await Resolve(screen, list, parent, ct) is not (var s, var l, _)) return NotFound();
            var item = l.Create(parent);
            var errors = await BindAsync(l.Item, item, isNew: true, ct);
            if (errors.Any)
            {
                Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                return View("Item", await ItemPage(s, l, item, isNew: true, errors, ct));
            }

            var siblings = await l.ListAsync(_store, parent, ct);
            item.SortOrder = siblings.Count == 0 ? 0 : siblings.Max(i => i.SortOrder) + 1;
            _store.Add(item);
            await _store.SaveChangesAsync(ct);

            if (l.Children.Count > 0)
            {
                Flash($"تمت إضافة {l.Item.Title}. يمكنك الآن إضافة {string.Join(" و", l.Children.Select(c => c.Title))}.");
                return Redirect(ItemUrl(s, l, item.Id));
            }
            Flash($"تمت إضافة {l.Item.Title}.");
            return Redirect(BackUrl(s, l, parent));
        }

        [HttpGet("{list}/{id:long}")]
        public async Task<IActionResult> Edit(string screen, string list, long id, CancellationToken ct)
        {
            if (await Item(screen, list, id, ct) is not (var s, var l, var item)) return NotFound();
            return View("Item", await ItemPage(s, l, item, isNew: false, new EditErrors(), ct));
        }

        [HttpPost("{list}/{id:long}")]
        public async Task<IActionResult> Update(string screen, string list, long id, CancellationToken ct)
        {
            if (await Item(screen, list, id, ct) is not (var s, var l, var item)) return NotFound();
            var name = l.Item.Summary(item);   // the saved name, for the page title if the form comes back
            var errors = await BindAsync(l.Item, item, isNew: false, ct);
            if (errors.Any)
            {
                Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                return View("Item", await ItemPage(s, l, item, isNew: false, errors, ct, name));
            }

            await _store.SaveChangesAsync(ct);
            Flash("تم حفظ التعديلات.");
            return Redirect(BackUrl(s, l, l.ParentIdOf(item)));
        }

        [HttpPost("{list}/{id:long}/delete")]
        public async Task<IActionResult> Delete(string screen, string list, long id, CancellationToken ct)
        {
            if (await Item(screen, list, id, ct) is not (var s, var l, var item)) return NotFound();
            var back = BackUrl(s, l, l.ParentIdOf(item));
            if (!l.CanDelete) return BadRequest();

            var usages = await _store.FindUsagesAsync(l.Item.EntityType, id, ct);
            if (usages.Count > 0)
            {
                FlashError($"لا يمكن حذف «{l.Item.Summary(item)}» لأنه مستخدم في: "
                           + string.Join("، ", usages.Select(u => $"{_registry.NameOf(u.EntityType)} ({u.Count})")) + ".");
                return Redirect(back);
            }

            await RemoveWithChildrenAsync(l, item, ct);
            await _store.SaveChangesAsync(ct);
            Flash($"تم حذف «{l.Item.Summary(item)}».");
            return Redirect(back);
        }

        [HttpPost("{list}/{id:long}/toggle")]
        public async Task<IActionResult> Toggle(string screen, string list, long id, CancellationToken ct)
        {
            if (await Item(screen, list, id, ct) is not (var s, var l, var item)) return NotFound();
            item.IsPublished = !item.IsPublished;
            await _store.SaveChangesAsync(ct);
            Flash(item.IsPublished ? $"أصبح «{l.Item.Summary(item)}» ظاهرًا في الموقع." : $"أُخفي «{l.Item.Summary(item)}» من الموقع.");
            return Redirect(BackUrl(s, l, l.ParentIdOf(item)));
        }

        [HttpPost("{list}/{id:long}/move")]
        public async Task<IActionResult> Move(string screen, string list, long id, bool up, CancellationToken ct)
        {
            if (await Item(screen, list, id, ct) is not (var s, var l, var item)) return NotFound();
            var parent = l.ParentIdOf(item);
            var siblings = await l.ListForUpdateAsync(_store, parent, ct);
            var index = siblings.FindIndex(i => i.Id == id);
            var target = up ? index - 1 : index + 1;
            if (index >= 0 && target >= 0 && target < siblings.Count)
            {
                (siblings[index], siblings[target]) = (siblings[target], siblings[index]);
                Renumber(siblings);
                await _store.SaveChangesAsync(ct);
            }
            return Redirect(BackUrl(s, l, parent));
        }

        /// <summary>The new order after a drag (all ids of the list, in order). JSON for the script, a redirect otherwise.</summary>
        [HttpPost("{list}/reorder")]
        public async Task<IActionResult> Reorder(string screen, string list, long? parent, [FromForm] long[] ids, CancellationToken ct)
        {
            if (await Resolve(screen, list, parent, ct) is not (var s, var l, _)) return NotFound();
            var siblings = await l.ListForUpdateAsync(_store, parent, ct);
            if (ids.Length != siblings.Count || !ids.Order().SequenceEqual(siblings.Select(i => i.Id).Order()))
                return Conflict(new { message = "تغيّرت القائمة؛ أعد تحميل الصفحة ثم أعد الترتيب." });

            var byId = siblings.ToDictionary(i => i.Id);
            Renumber(ids.Select(i => byId[i]).ToList());
            await _store.SaveChangesAsync(ct);
            if (Request.Headers.Accept.Any(a => a?.Contains("application/json") == true)) return Json(new { ok = true });
            Flash("تم حفظ الترتيب.");
            return Redirect(BackUrl(s, l, parent));
        }

        // ------------------------------------------------------------ helpers

        private EditorScreen? Screen(string slug) =>
            _registry.Find(slug) is { } s && (!s.AdminOnly || User.IsInRole(AdminRoles.Admin)) ? s : null;

        /// <summary>The screen and list of a URL; for a child list the parent item must exist.</summary>
        private async Task<(EditorScreen, ListEditor, ContentItem?)?> Resolve(string screen, string list, long? parent, CancellationToken ct)
        {
            if (Screen(screen) is not { } s || s.FindList(list) is not { } l) return null;
            if (l.Parent is null) return parent is null ? (s, l, null) : null;
            if (parent is null || await l.Parent.FindAsync(_store, parent.Value, ct) is not { } owner) return null;
            return (s, l, owner);
        }

        private async Task<(EditorScreen, ListEditor, ContentItem)?> Item(string screen, string list, long id, CancellationToken ct)
        {
            if (Screen(screen) is not { } s || s.FindList(list) is not { } l) return null;
            return await l.FindAsync(_store, id, ct) is { } item ? (s, l, item) : null;
        }

        private async Task<EditErrors> BindAsync(EntityEditor editor, BaseEntity entity, bool isNew, CancellationToken ct)
        {
            var errors = new EditErrors();
            var form = await Request.ReadFormAsync(ct);
            if (!isNew && (!long.TryParse(form[StampField], out var stamp) || stamp != entity.LastModifiedDate.Ticks))
                errors.Add("", ConcurrencyMessage);

            var options = await OptionsAsync(editor, ct);
            foreach (var field in editor.Fields)
                await FieldBinder.BindAsync(field, entity, form, errors, options.GetValueOrDefault(field.Name) ?? [], _store, ct);
            if (!errors.Any) await editor.ValidateAsync(entity, new EditContext(_store, errors, isNew, ct));
            return errors;
        }

        private async Task<Dictionary<string, IReadOnlyList<FieldOption>>> OptionsAsync(EntityEditor editor, CancellationToken ct)
        {
            var options = new Dictionary<string, IReadOnlyList<FieldOption>>();
            foreach (var field in editor.Fields.Where(f => f.Kind == FieldKind.Select))
                options[field.Name] = await field.OptionsAsync(_store, ct);
            return options;
        }

        private async Task<EditorFormModel> FormModel(EntityEditor editor, BaseEntity entity, bool isNew, EditErrors errors,
            string postUrl, string? cancelUrl, CancellationToken ct)
        {
            var media = new Dictionary<long, MediaAsset>();
            foreach (var field in editor.Fields.Where(f => f.Kind == FieldKind.Media))
                if (field.Property.GetValue(entity) is long id && !media.ContainsKey(id) && await _store.FindAsync<MediaAsset>(id, ct) is { } asset)
                    media[id] = asset;

            return new EditorFormModel
            {
                Editor = editor,
                Entity = entity,
                IsNew = isNew,
                Errors = errors,
                PostUrl = postUrl,
                CancelUrl = cancelUrl,
                Options = await OptionsAsync(editor, ct),
                Media = media
            };
        }

        private async Task<ScreenPageModel> ScreenPage(EditorScreen s, BaseEntity? entity, EditErrors errors, CancellationToken ct)
        {
            var lists = new List<ListModel>();
            foreach (var list in s.Lists) lists.Add(await ListModel(s, list, null, ct));
            return new ScreenPageModel
            {
                Screen = s,
                Form = s.Main is null || entity is null
                    ? null
                    : await FormModel(s.Main, entity, entity.Id == 0, errors, ScreenUrl(s), null, ct),
                Lists = lists
            };
        }

        private async Task<ItemPageModel> ItemPage(EditorScreen s, ListEditor l, ContentItem item, bool isNew, EditErrors errors,
            CancellationToken ct, string? savedName = null)
        {
            var parent = l.ParentIdOf(item);
            var children = new List<ListModel>();
            if (!isNew)
                foreach (var child in l.Children) children.Add(await ListModel(s, child, item.Id, ct));

            var crumbs = new List<Crumb> { new(s.Title, ScreenUrl(s)) };
            if (l.Parent is not null && parent is not null && await l.Parent.FindAsync(_store, parent.Value, ct) is { } owner)
                crumbs.Add(new Crumb(l.Parent.Item.Summary(owner), ItemUrl(s, l.Parent, owner.Id)));
            var title = isNew ? $"إضافة {l.Item.Title}" : savedName ?? l.Item.Summary(item);
            crumbs.Add(new Crumb(title, null));

            var postUrl = isNew
                ? $"/admin/sections/{s.Slug}/{l.Path}/new" + (parent is null ? "" : $"?parent={parent}")
                : ItemUrl(s, l, item.Id);
            return new ItemPageModel
            {
                Screen = s,
                List = l,
                Title = title,
                Crumbs = crumbs,
                Children = children,
                Form = await FormModel(l.Item, item, isNew, errors, postUrl, BackUrl(s, l, parent), ct)
            };
        }

        private async Task<ListModel> ListModel(EditorScreen s, ListEditor list, long? parentId, CancellationToken ct)
        {
            var columns = new List<(string, Func<ContentItem, string>)>();
            foreach (var column in list.Columns) columns.Add((column.Header, await column.Prepare(_store, ct)));
            return new ListModel
            {
                Screen = s,
                List = list,
                ParentId = parentId,
                Items = await list.ListAsync(_store, parentId, ct),
                Columns = columns
            };
        }

        private async Task RemoveWithChildrenAsync(ListEditor list, ContentItem item, CancellationToken ct)
        {
            foreach (var child in list.Children)
                foreach (var row in await child.ListForUpdateAsync(_store, item.Id, ct))
                    await RemoveWithChildrenAsync(child, row, ct);
            _store.Remove(item);
        }

        private static void Renumber(List<ContentItem> items)
        {
            for (var i = 0; i < items.Count; i++)
                if (items[i].SortOrder != i) items[i].SortOrder = i;
        }

        private static string ScreenUrl(EditorScreen s) => $"/admin/sections/{s.Slug}";

        private static string ItemUrl(EditorScreen s, ListEditor l, long id) => $"/admin/sections/{s.Slug}/{l.Path}/{id}";

        /// <summary>Where a list item's page returns to: its parent item, or the screen at the list.</summary>
        private static string BackUrl(EditorScreen s, ListEditor l, long? parentId) =>
            l.Parent is not null && parentId is not null
                ? $"{ItemUrl(s, l.Parent, parentId.Value)}#list-{l.Path.Replace('.', '-')}"
                : $"{ScreenUrl(s)}#list-{l.Path.Replace('.', '-')}";
    }
}
