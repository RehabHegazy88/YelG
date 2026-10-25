using Microsoft.AspNetCore.Mvc;
using YelG.WebSite.Application.Content;
using YelG.WebSite.Localization;
using YelG.WebSite.Models;

namespace YelG.WebSite.Controllers
{
    /// <summary>The news and insights page: every post of the insights section (each opens where it was published).</summary>
    [Route("/{lang:" + SiteLanguage.RouteConstraint + "}/insights")]
    public sealed class InsightsController : Controller
    {
        private readonly HomeContentService _content;
        private readonly UiStrings _ui;
        private readonly IConfiguration _configuration;

        public InsightsController(HomeContentService content, UiStrings ui, IConfiguration configuration)
        {
            _content = content;
            _ui = ui;
            _configuration = configuration;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string lang, string? category, CancellationToken ct)
        {
            var content = await _content.GetAsync(cancellationToken: ct);
            if (content.Insights is null) return NotFound();
            var baseUrl = (_configuration["Site:BaseUrl"] is { Length: > 0 } configured ? configured : $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
            return View(new InsightsPageModel(content, lang, _ui, baseUrl, category));
        }
    }
}
