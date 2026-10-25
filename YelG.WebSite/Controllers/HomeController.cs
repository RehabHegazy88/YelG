using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using YelG.WebSite.Application.Content;
using YelG.WebSite.Localization;
using YelG.WebSite.Models;

namespace YelG.WebSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly HomeContentService _content;
        private readonly UiStrings _ui;
        private readonly IConfiguration _configuration;

        public HomeController(HomeContentService content, UiStrings ui, IConfiguration configuration)
        {
            _content = content;
            _ui = ui;
            _configuration = configuration;
        }

        /// <summary>"/" sends visitors to the default language chosen in the site settings.</summary>
        [HttpGet("/")]
        public async Task<IActionResult> Root(CancellationToken ct)
        {
            var content = await _content.GetAsync(cancellationToken: ct);
            var lang = SiteLanguage.IsSupported(content.Site.DefaultLanguage) ? content.Site.DefaultLanguage : SiteLanguage.Arabic;
            return Redirect($"/{lang}");
        }

        [HttpGet("/{lang:" + SiteLanguage.RouteConstraint + "}")]
        public async Task<IActionResult> Index(string lang, CancellationToken ct)
        {
            var content = await _content.GetAsync(cancellationToken: ct);
            return View(new HomePageModel(content, lang, _ui, preview: false, BaseUrl()));
        }

        [HttpGet("/sitemap.xml")]
        public async Task<IActionResult> Sitemap(CancellationToken ct)
        {
            var content = await _content.GetAsync(cancellationToken: ct);
            var modified = content.Site.LastModifiedDate == default ? "" : $"<lastmod>{content.Site.LastModifiedDate:yyyy-MM-dd}</lastmod>";
            var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n")
                .Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">\n");
            // the homepage, the news page, the careers page and each open role, in both languages
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var paths = new List<string> { "" };
            if (content.Insights is not null) paths.Add("/insights");
            if (content.Careers is not null)
            {
                paths.Add("/careers");
                paths.AddRange(content.CareerRoles.Where(r => r.IsOpenOn(today)).Select(r => "/careers/" + Uri.EscapeDataString(r.Slug)));
            }
            foreach (var path in paths)
                foreach (var lang in SiteLanguage.All)
                {
                    xml.Append("<url><loc>").Append(BaseUrl()).Append('/').Append(lang).Append(path).Append("</loc>").Append(path.Length == 0 ? modified : "");
                    foreach (var alt in SiteLanguage.All)
                        xml.Append("<xhtml:link rel=\"alternate\" hreflang=\"").Append(alt).Append("\" href=\"").Append(BaseUrl()).Append('/').Append(alt).Append(path).Append("\"/>");
                    xml.Append("</url>\n");
                }
            xml.Append("</urlset>\n");
            return Content(xml.ToString(), "application/xml", Encoding.UTF8);
        }

        [HttpGet("/robots.txt")]
        public IActionResult Robots() =>
            Content($"User-agent: *\nDisallow: /admin\nAllow: /\n\nSitemap: {BaseUrl()}/sitemap.xml\n", "text/plain", Encoding.UTF8);

        [Route("/error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

        /// <summary>The public address: "Site:BaseUrl" when configured (behind a proxy), else this request's host.</summary>
        private string BaseUrl() =>
            (_configuration["Site:BaseUrl"] is { Length: > 0 } configured ? configured : $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
    }
}
