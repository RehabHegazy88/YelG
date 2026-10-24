using Microsoft.AspNetCore.Mvc;
using YelG.WebSite.Application.Content;
using YelG.WebSite.Localization;
using YelG.WebSite.Models;

namespace YelG.WebSite.Areas.Admin.Controllers
{
    /// <summary>The homepage as it will look, hidden sections and items included, for signed-in editors only (never indexed or cached).</summary>
    [Route("admin/preview")]
    public sealed class PreviewController : AdminController
    {
        private readonly HomeContentService _content;
        private readonly UiStrings _ui;
        private readonly IConfiguration _configuration;

        public PreviewController(HomeContentService content, UiStrings ui, IConfiguration configuration)
        {
            _content = content;
            _ui = ui;
            _configuration = configuration;
        }

        [HttpGet("{lang:" + SiteLanguage.RouteConstraint + "}")]
        public async Task<IActionResult> Index(string lang, CancellationToken ct)
        {
            var content = await _content.GetAsync(preview: true, ct);
            var baseUrl = _configuration["Site:BaseUrl"] is { Length: > 0 } configured ? configured : $"{Request.Scheme}://{Request.Host}";
            Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
            return View("~/Views/Home/Index.cshtml", new HomePageModel(content, lang, _ui, preview: true, baseUrl));
        }
    }
}
