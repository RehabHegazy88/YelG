using Microsoft.AspNetCore.Mvc;
using YelG.WebSite.Application.Content;
using YelG.WebSite.Localization;
using YelG.WebSite.Models;

namespace YelG.WebSite.Controllers
{
    /// <summary>The news and insights page (every post of the insights section) and each post's own page.</summary>
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
            return View(new InsightsPageModel(content, lang, _ui, BaseUrl, category));
        }

        /// <summary>A post's own page; a post without one goes to where it was published.</summary>
        [HttpGet("{slug}")]
        public async Task<IActionResult> Details(string lang, string slug, CancellationToken ct)
        {
            var content = await _content.GetAsync(cancellationToken: ct);
            var post = content.Insights is null ? null : content.InsightPosts.FirstOrDefault(p => p.Slug == slug);
            if (post is null) return NotFound();
            if (!post.HasPage)
                return Uri.TryCreate(post.ExternalUrl, UriKind.Absolute, out var external) && external.Scheme == Uri.UriSchemeHttps
                    ? Redirect(external.ToString()) : NotFound();
            return View(new InsightsPageModel(content, lang, _ui, BaseUrl, null, post));
        }

        private string BaseUrl =>
            (_configuration["Site:BaseUrl"] is { Length: > 0 } configured ? configured : $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
    }
}
