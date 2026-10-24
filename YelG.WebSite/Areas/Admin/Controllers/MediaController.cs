using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Application.Media;
using YelG.WebSite.Areas.Admin.Editing;
using YelG.WebSite.Areas.Admin.Models;
using YelG.WebSite.Domain.Media;
using YelG.WebSite.Infrastructure.Data;

namespace YelG.WebSite.Areas.Admin.Controllers
{
    /// <summary>The media library: upload, browse, describe and delete images and videos, and the picker the editors use.</summary>
    [Route("admin/media")]
    public sealed class MediaController : AdminController
    {
        private const int PageSize = 48;

        private readonly WebSiteDbContext _db;
        private readonly IContentStore _store;
        private readonly MediaUploadService _uploads;
        private readonly EditorRegistry _registry;
        private readonly MediaOptions _options;

        public MediaController(WebSiteDbContext db, IContentStore store, MediaUploadService uploads, EditorRegistry registry,
            IOptions<MediaOptions> options)
        {
            _db = db;
            _store = store;
            _uploads = uploads;
            _registry = registry;
            _options = options.Value;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(MediaKind? kind, string? q, int page = 1, CancellationToken ct = default)
        {
            var query = Filter(kind, q);
            var total = await query.CountAsync(ct);
            var pages = Math.Max(1, (total + PageSize - 1) / PageSize);
            page = Math.Clamp(page, 1, pages);
            var items = await query.OrderByDescending(m => m.AddedDate).ThenByDescending(m => m.Id)
                .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(ct);
            return View(new MediaLibraryModel
            {
                Items = items, Kind = kind, Query = q, Page = page, PageCount = pages, Total = total,
                MaxImage = MediaUploadService.FormatSize(_options.MaxImageBytes),
                MaxVideo = MediaUploadService.FormatSize(_options.MaxVideoBytes)
            });
        }

        /// <summary>Uploads one or more files. The picker and the drop zone ask for JSON; the plain form gets a redirect.</summary>
        [HttpPost("upload")]
        [RequestSizeLimit(250L * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 250L * 1024 * 1024)]
        public async Task<IActionResult> Upload(List<IFormFile> files, CancellationToken ct)
        {
            var results = new List<object>();
            var errors = new List<string>();
            foreach (var file in files)
            {
                await using var stream = file.OpenReadStream();
                var result = await _uploads.UploadAsync(stream, file.FileName, ct);
                if (result.Asset is { } asset) results.Add(ToJson(asset));
                else errors.Add($"{file.FileName}: {result.Error}");
            }
            if (files.Count == 0) errors.Add("اختر ملفًا لرفعه.");

            if (WantsJson) return errors.Count > 0 && results.Count == 0
                ? UnprocessableEntity(new { errors })
                : Ok(new { items = results, errors });

            if (results.Count > 0) Flash(results.Count == 1 ? "تم رفع الملف." : $"تم رفع {results.Count} ملفات.");
            if (errors.Count > 0) FlashError(string.Join(" ", errors));
            return Redirect("/admin/media");
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> Details(long id, CancellationToken ct)
        {
            if (await _store.FindAsync<MediaAsset>(id, ct) is not { } asset) return NotFound();
            return View(await DetailsModel(asset, new EditErrors(), ct));
        }

        [HttpPost("{id:long}")]
        public async Task<IActionResult> Update(long id, CancellationToken ct)
        {
            if (await _store.FindAsync<MediaAsset>(id, ct) is not { } asset) return NotFound();
            var form = await Request.ReadFormAsync(ct);
            asset.Alt.Ar = form["Alt.Ar"].ToString().Trim();
            asset.Alt.En = form["Alt.En"].ToString().Trim();
            var errors = new EditErrors();
            if (asset.Alt.Ar.Length > 500) errors.Add("Alt.Ar", "الحد الأقصى 500 حرف.");
            if (asset.Alt.En.Length > 500) errors.Add("Alt.En", "الحد الأقصى 500 حرف.");
            if (errors.Any) return View("Details", await DetailsModel(asset, errors, ct));

            await _store.SaveChangesAsync(ct);
            Flash("تم حفظ الوصف.");
            return Redirect($"/admin/media/{id}");
        }

        [HttpPost("{id:long}/delete")]
        public async Task<IActionResult> Delete(long id, CancellationToken ct)
        {
            if (await _store.FindAsync<MediaAsset>(id, ct) is not { } asset) return NotFound();
            var usages = await _store.FindUsagesAsync(typeof(MediaAsset), id, ct);
            if (usages.Count > 0)
            {
                FlashError("لا يمكن حذف الملف لأنه مستخدم في: " + string.Join("، ", usages.Select(u => _registry.NameOf(u.EntityType))) + ". اختر ملفًا آخر هناك أولًا.");
                return Redirect($"/admin/media/{id}");
            }
            // the file stays on disk (soft delete), so the change can be undone from the database if needed
            _store.Remove(asset);
            await _store.SaveChangesAsync(ct);
            Flash("تم حذف الملف من المكتبة.");
            return Redirect("/admin/media");
        }

        /// <summary>The picker's list: newest first, filtered by kind and name.</summary>
        [HttpGet("pick")]
        public async Task<IActionResult> Pick(MediaKind kind, string? q, CancellationToken ct)
        {
            var items = await Filter(kind, q).OrderByDescending(m => m.AddedDate).ThenByDescending(m => m.Id).Take(200).ToListAsync(ct);
            return Ok(new { items = items.Select(ToJson) });
        }

        private IQueryable<MediaAsset> Filter(MediaKind? kind, string? q)
        {
            var query = _db.MediaAssets.AsNoTracking();
            if (kind is not null) query = query.Where(m => m.Kind == kind);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(m => m.OriginalFileName.Contains(term) || m.Alt.Ar.Contains(term) || m.Alt.En.Contains(term));
            }
            return query;
        }

        private async Task<MediaDetailsModel> DetailsModel(MediaAsset asset, EditErrors errors, CancellationToken ct)
        {
            var usages = await _store.FindUsagesAsync(typeof(MediaAsset), asset.Id, ct);
            return new MediaDetailsModel
            {
                Asset = asset,
                Errors = errors,
                Usages = usages.Select(u => (_registry.NameOf(u.EntityType), _registry.ScreenOf(u.EntityType) is { } s ? $"/admin/sections/{s.Slug}" : null))
                    .Distinct().ToList()
            };
        }

        private bool WantsJson => Request.Headers.Accept.Any(a => a?.Contains("application/json") == true);

        private static object ToJson(MediaAsset m) => new
        {
            id = m.Id,
            kind = m.Kind == MediaKind.Video ? "video" : "image",
            name = m.OriginalFileName,
            url = m.Path,
            thumb = m.ThumbnailPath ?? (m.Kind == MediaKind.Image ? m.Path : null),
            width = m.Width,
            height = m.Height,
            size = MediaUploadService.FormatSize(m.SizeBytes),
            alt = m.Alt.Ar
        };
    }
}
