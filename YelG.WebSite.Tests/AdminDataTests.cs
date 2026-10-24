using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using YelG.WebSite.Application;
using YelG.WebSite.Application.Abstractions;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Application.Media;
using YelG.WebSite.Areas.Admin.Editing;
using YelG.WebSite.Domain.Media;
using YelG.WebSite.Domain.Sections;
using YelG.WebSite.Infrastructure;
using YelG.WebSite.Infrastructure.Data;
using YelG.WebSite.Infrastructure.Identity;
using YelG.WebSite.Infrastructure.Seeding;

namespace YelG.WebSite.Tests
{
    /// <summary>The admin panel's data operations against the test database: uploads, usages, the marquee task.</summary>
    [Collection(WebSiteDatabaseCollection.Name)]
    public sealed class AdminDataTests : IDisposable
    {
        private readonly string _mediaRoot = Path.Combine(Path.GetTempPath(), "yelg-media-tests-" + Guid.NewGuid().ToString("N"));

        public AdminDataTests(WebSiteDatabase db) => _ = db;

        public void Dispose()
        {
            if (Directory.Exists(_mediaRoot)) Directory.Delete(_mediaRoot, recursive: true);
        }

        private ServiceProvider Site(long maxImageBytes = 10 * 1024 * 1024)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICurrentUser>(new TestUser(null));
            services.AddWebSiteApplication();
            services.AddWebSiteInfrastructure(WebSiteDatabase.ConnectionString);
            services.Configure<MediaOptions>(o =>
            {
                o.StoragePath = _mediaRoot;
                o.MaxImageBytes = maxImageBytes;
            });
            services.AddIdentityCore<AdminUser>(IdentityPolicy.Configure).AddRoles<AdminRole>().AddEntityFrameworkStores<WebSiteDbContext>();
            return services.BuildServiceProvider();
        }

        private static async Task SeedAsync(IServiceProvider site)
        {
            await using var scope = site.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ContentSeeder>().SeedAsync(new SeedOptions { AdminEmail = "admin@test.local", AdminPassword = "seed-pass-123" });
        }

        private static MemoryStream Png(int width, int height)
        {
            using var bitmap = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(bitmap)) canvas.Clear(new SKColor(0x0A, 0x31, 0x77));
            using var data = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100);
            return new MemoryStream(data.ToArray());
        }

        [Fact]
        public void File_types_are_recognised_by_their_content()
        {
            Assert.Equal(MediaFileType.Png, MediaFileType.Detect(Png(4, 4).ToArray()));
            Assert.Equal(MediaFileType.Jpeg, MediaFileType.Detect(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }));
            Assert.Equal(MediaFileType.WebP, MediaFileType.Detect("RIFF\0\0\0\0WEBPVP8 "u8));
            Assert.Equal(MediaFileType.Mp4, MediaFileType.Detect("\0\0\0\u0018ftypmp42"u8));
            Assert.Null(MediaFileType.Detect("<svg xmlns=\"http://www.w3.org/2000/svg\">"u8));
            Assert.Null(MediaFileType.Detect("MZ\u0090\0"u8));
        }

        [LocalDbFact]
        public async Task An_upload_is_kept_with_its_size_and_a_webp_thumbnail()
        {
            await using var site = Site();
            await using var scope = site.CreateAsyncScope();
            var uploads = scope.ServiceProvider.GetRequiredService<MediaUploadService>();

            var result = await uploads.UploadAsync(Png(1200, 800), @"C:\photos\team photo.png");

            var asset = Assert.IsType<MediaAsset>(result.Asset);
            Assert.Equal(MediaKind.Image, asset.Kind);
            Assert.Equal("team photo.png", asset.OriginalFileName);
            Assert.Equal((1200, 800), (asset.Width, asset.Height));
            Assert.StartsWith("/media/", asset.Path);
            Assert.EndsWith(".png", asset.Path);

            var thumbnail = Path.Combine(_mediaRoot, asset.ThumbnailPath!["/media/".Length..].Replace('/', Path.DirectorySeparatorChar));
            using var thumb = SKBitmap.Decode(thumbnail);
            Assert.Equal((480, 320), (thumb.Width, thumb.Height));
            Assert.Equal(MediaFileType.WebP, MediaFileType.Detect(File.ReadAllBytes(thumbnail)));
        }

        [LocalDbFact]
        public async Task Uploads_of_other_types_or_too_large_are_refused_and_nothing_is_written()
        {
            await using var site = Site(maxImageBytes: 100);
            await using var scope = site.CreateAsyncScope();
            var uploads = scope.ServiceProvider.GetRequiredService<MediaUploadService>();

            var svg = await uploads.UploadAsync(new MemoryStream("<svg onload=\"alert(1)\"/>"u8.ToArray()), "x.svg");
            var renamed = await uploads.UploadAsync(new MemoryStream("GIF89a, but not really"u8.ToArray()), "fake.gif");
            var large = await uploads.UploadAsync(Png(800, 800), "large.png");

            Assert.False(svg.Succeeded);
            Assert.False(renamed.Succeeded);
            Assert.False(large.Succeeded);
            Assert.Contains("1 KB", large.Error);
            Assert.False(Directory.Exists(_mediaRoot) && Directory.EnumerateFiles(_mediaRoot, "*", SearchOption.AllDirectories).Any());
        }

        [LocalDbFact]
        public async Task A_row_in_use_reports_where_it_is_used()
        {
            await using var site = Site();
            await SeedAsync(site);
            await using var scope = site.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IContentStore>();
            var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();

            var heroImage = (await db.HeroSections.AsNoTracking().FirstAsync()).ImageId!.Value;
            Assert.Contains(await store.FindUsagesAsync(typeof(MediaAsset), heroImage), u => u.EntityType == typeof(HeroSection));

            var category = await db.ServiceCategories.AsNoTracking().OrderBy(c => c.SortOrder).FirstAsync();
            var usage = Assert.Single(await store.FindUsagesAsync(typeof(ServiceCategory), category.Id));
            Assert.Equal(typeof(Service), usage.EntityType);
            Assert.Equal(await db.Services.CountAsync(s => s.CategoryId == category.Id), usage.Count);

            // its own deliverables do not keep a service from being deleted; the self-check and the engagement cards do
            var service = await db.Services.AsNoTracking().FirstAsync(s => s.Key == "acc");
            var serviceUsages = await store.FindUsagesAsync(typeof(Service), service.Id);
            Assert.DoesNotContain(serviceUsages, u => u.EntityType == typeof(ServiceDeliverable));
            Assert.Contains(serviceUsages, u => u.EntityType == typeof(SelfCheckQuestion));
        }

        [LocalDbFact]
        public async Task The_marquee_can_be_rebuilt_from_the_visible_services()
        {
            await using var site = Site();
            await SeedAsync(site);
            var action = AdminScreens.Build().Find("marquee")!.Actions.Single();
            List<long> original;

            await using (var scope = site.CreateAsyncScope())
            {
                var store = scope.ServiceProvider.GetRequiredService<IContentStore>();
                original = (await store.ListAsync<MarqueeItem>()).Select(m => m.Id).ToList();
                await action.Run(store, CancellationToken.None);
            }

            await using (var scope = site.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
                var labels = await db.MarqueeItems.OrderBy(m => m.SortOrder).Select(m => m.Label.En).ToListAsync();
                var services = await db.Services.Where(s => s.IsPublished && s.Category.IsPublished)
                    .OrderBy(s => s.Category.SortOrder).ThenBy(s => s.SortOrder).ThenBy(s => s.Id).Select(s => s.Name.En).ToListAsync();
                Assert.Equal(services, labels);

                // put the seeded strip back for the other tests
                await db.MarqueeItems.ExecuteDeleteAsync();
                await db.MarqueeItems.IgnoreQueryFilters().Where(m => original.Contains(m.Id))
                    .ExecuteUpdateAsync(s => s.SetProperty(m => m.Deleted, false).SetProperty(m => m.DeleteDate, (DateTime?)null));
            }
        }
    }
}
