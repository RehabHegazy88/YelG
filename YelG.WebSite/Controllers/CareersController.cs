using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using YelG.WebSite.Application.Careers;
using YelG.WebSite.Application.Content;
using YelG.WebSite.Infrastructure;
using YelG.WebSite.Localization;
using YelG.WebSite.Models;
using YelG.WebSite.Rendering;

namespace YelG.WebSite.Controllers
{
    /// <summary>
    /// The careers pages: the open roles (/{lang}/careers), one role (/{lang}/careers/{slug}) and its application
    /// form. The page script sends the form and shows the answer in place; without script the browser posts it
    /// and comes back to the role's page with the outcome.
    /// </summary>
    [Route("/{lang:" + SiteLanguage.RouteConstraint + "}/careers")]
    public sealed class CareersController : Controller
    {
        public const string OutcomeKey = "application.outcome";

        private readonly HomeContentService _content;
        private readonly JobApplicationService _applications;
        private readonly UiStrings _ui;
        private readonly IConfiguration _configuration;
        private readonly TimeProvider _clock;
        private readonly ILogger<CareersController> _logger;

        public CareersController(HomeContentService content, JobApplicationService applications, UiStrings ui, IConfiguration configuration,
            TimeProvider clock, ILogger<CareersController> logger)
        {
            _content = content;
            _applications = applications;
            _ui = ui;
            _configuration = configuration;
            _clock = clock;
            _logger = logger;
        }

        private DateOnly Today => DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);

        private string BaseUrl =>
            (_configuration["Site:BaseUrl"] is { Length: > 0 } configured ? configured : $"{Request.Scheme}://{Request.Host}").TrimEnd('/');

        [HttpGet("")]
        public async Task<IActionResult> Index(string lang, string? department, CancellationToken ct)
        {
            var content = await _content.GetAsync(cancellationToken: ct);
            if (content.Careers is null) return NotFound();
            return View(new CareersPageModel(content, lang, _ui, BaseUrl, Today, null, department, _applications.MaxCvBytes));
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> Details(string lang, string slug, CancellationToken ct)
        {
            var content = await _content.GetAsync(cancellationToken: ct);
            var role = content.Careers is null ? null : content.CareerRoles.FirstOrDefault(r => r.Slug == slug && r.IsOpenOn(Today));
            if (role is null) return NotFound();
            return View(new CareersPageModel(content, lang, _ui, BaseUrl, Today, role, null, _applications.MaxCvBytes));
        }

        [HttpPost("{slug}/apply")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimits.Application)]
        [RequestSizeLimit(8 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 8 * 1024 * 1024)]
        public Task<IActionResult> Apply(string lang, string slug, [FromForm] ApplicationInput input, IFormFile? cv, CancellationToken ct) =>
            SubmitAsync(lang, slug, input, cv, ct);

        /// <summary>A general application: a CV for future roles, from the careers page.</summary>
        [HttpPost("apply")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimits.Application)]
        [RequestSizeLimit(8 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 8 * 1024 * 1024)]
        public Task<IActionResult> ApplyGeneral(string lang, [FromForm] ApplicationInput input, IFormFile? cv, CancellationToken ct) =>
            SubmitAsync(lang, null, input, cv, ct);

        private async Task<IActionResult> SubmitAsync(string lang, string? slug, ApplicationInput input, IFormFile? cv, CancellationToken ct)
        {
            var content = await _content.GetAsync(cancellationToken: ct);
            if (content.Careers is null) return NotFound();
            var consent = RichText.Plain(CareerTexts.Or(content.Careers.ApplyConsentText, "consent", lang));
            ApplicationResult result;
            try
            {
                await using var stream = cv?.OpenReadStream();
                var upload = cv is null || stream is null ? null : new CvUpload(stream, cv.FileName, cv.Length);
                result = await _applications.SubmitAsync(slug, input, upload, lang, consent,
                    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), ct,
                    generalTitle: CareerTexts.Get("generalApplication", lang));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "A job application could not be saved");
                return Answer(lang, slug, StatusCodes.Status500InternalServerError, "failed");
            }

            switch (result.Outcome)
            {
                case ApplicationOutcome.Invalid:
                    if (!WantsJson) return Answer(lang, slug, StatusCodes.Status422UnprocessableEntity, "invalid");
                    return UnprocessableEntity(new { ok = false, errors = result.Errors.ToDictionary(e => FieldOf(e), e => Message(e, lang)) });
                case ApplicationOutcome.RoleClosed:
                    return WantsJson
                        ? StatusCode(StatusCodes.Status410Gone, new { ok = false, message = CareerTexts.Get("closed", lang) })
                        : Redirect($"/{lang}/careers");
                case ApplicationOutcome.Saved:
                    _logger.LogInformation("Job application {ApplicationId} saved", result.ApplicationId);
                    break;
            }
            return Answer(lang, slug, StatusCodes.Status200OK, "sent");
        }

        private bool WantsJson => Request.Headers.Accept.Any(a => a?.Contains("application/json") == true);

        /// <summary>The form field a rule belongs to (the three CV rules all point at the file field).</summary>
        private static string FieldOf(string error) => error.StartsWith("cv", StringComparison.Ordinal) ? "cv" : error;

        private string Message(string error, string lang) => error switch
        {
            "name" or "email" or "phone" => _ui.Validation(lang).GetValueOrDefault(error, error),
            _ => CareerTexts.Get("err." + error, lang).Replace("{max}", $"{_applications.MaxCvBytes / (1024 * 1024)} {(lang == "en" ? "MB" : "ميجابايت")}")
        };

        private IActionResult Answer(string lang, string? slug, int status, string outcome)
        {
            if (WantsJson) return StatusCode(status, new { ok = status == StatusCodes.Status200OK });
            TempData[OutcomeKey] = outcome;
            return Redirect(slug is null ? $"/{lang}/careers#apply" : $"/{lang}/careers/{slug}#apply");
        }
    }
}
