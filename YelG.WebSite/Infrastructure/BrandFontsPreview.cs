using Microsoft.Extensions.FileProviders;

namespace YelG.WebSite.Infrastructure
{
    /// <summary>
    /// A look at the site in the brand typefaces (Gotham, Expo Arabic) on a developer's machine only, while the
    /// company has no web licence for them. The font files stay in App_Data/brand-fonts (ignored by git and not
    /// part of a deployment) with a fonts.css that maps the design's families to them. It is on only in
    /// Development with "Site:BrandFontsPreview" = true; otherwise the design's Google fonts load as usual.
    /// </summary>
    public sealed class BrandFontsPreview
    {
        public const string RequestPath = "/brand-fonts-preview";

        public BrandFontsPreview(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Folder = Path.Combine(environment.ContentRootPath, "App_Data", "brand-fonts");
            Enabled = environment.IsDevelopment() && configuration.GetValue<bool>("Site:BrandFontsPreview")
                      && File.Exists(Path.Combine(Folder, "fonts.css"));
        }

        public bool Enabled { get; }

        public string Folder { get; }

        public string StylesheetUrl => RequestPath + "/fonts.css";

        public void Map(IApplicationBuilder app)
        {
            if (!Enabled) return;
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Folder),
                RequestPath = RequestPath,
                OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-store"
            });
        }
    }
}
