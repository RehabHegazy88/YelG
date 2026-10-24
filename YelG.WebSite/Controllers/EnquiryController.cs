using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using YelG.WebSite.Application.Enquiries;
using YelG.WebSite.Infrastructure;
using YelG.WebSite.Localization;

namespace YelG.WebSite.Controllers
{
    /// <summary>
    /// The consultation form's backend. The page script posts here and shows the answer in place; without
    /// script the browser posts the form and comes back to the form with the outcome.
    /// </summary>
    public sealed class EnquiryController : Controller
    {
        public const string OutcomeKey = "enquiry.outcome";

        private readonly EnquiryService _enquiries;
        private readonly UiStrings _ui;
        private readonly ILogger<EnquiryController> _logger;

        public EnquiryController(EnquiryService enquiries, UiStrings ui, ILogger<EnquiryController> logger)
        {
            _enquiries = enquiries;
            _ui = ui;
            _logger = logger;
        }

        [HttpPost("/{lang:" + SiteLanguage.RouteConstraint + "}/enquiry")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimits.Enquiry)]
        [RequestSizeLimit(64 * 1024)]
        public async Task<IActionResult> Submit(string lang, [FromForm] EnquiryInput input, CancellationToken ct)
        {
            var origin = new EnquiryOrigin(lang, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            EnquiryResult result;
            try
            {
                result = await _enquiries.SubmitAsync(input, origin, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "An enquiry could not be saved");
                return Answer(lang, StatusCodes.Status500InternalServerError, "failed");
            }

            if (result.Outcome == EnquiryOutcome.Invalid)
            {
                var messages = _ui.Validation(lang);
                if (!WantsJson) return Answer(lang, StatusCodes.Status422UnprocessableEntity, "invalid");
                return UnprocessableEntity(new { ok = false, errors = result.Errors.ToDictionary(e => e, e => messages.GetValueOrDefault(e, e)) });
            }

            if (result.Outcome == EnquiryOutcome.Saved) _logger.LogInformation("Enquiry {EnquiryId} saved", result.EnquiryId);
            return Answer(lang, StatusCodes.Status200OK, "sent");
        }

        private bool WantsJson => Request.Headers.Accept.Any(a => a?.Contains("application/json") == true);

        /// <summary>JSON for the page script; for a plain form post, back to the form with the outcome.</summary>
        private IActionResult Answer(string lang, int status, string outcome)
        {
            if (WantsJson) return StatusCode(status, new { ok = status == StatusCodes.Status200OK });
            TempData[OutcomeKey] = outcome;
            return Redirect($"/{lang}#consult");
        }
    }
}
