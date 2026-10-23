using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using YelG.BLL.Common;

namespace YelG.AdminPortal.Infrastructure
{
    /// <summary>
    /// Turns business exceptions into user-facing results: JSON { error } for AJAX calls,
    /// otherwise an error message shown on the page the user came from.
    /// </summary>
    public sealed class BusinessExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is not YelgBusinessException ex) return;

            var status = ex switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                ForbiddenException => StatusCodes.Status403Forbidden,
                ConcurrencyException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            if (context.HttpContext.Request.IsAjax())
            {
                context.Result = new JsonResult(new { error = ex.Message }) { StatusCode = status };
            }
            else
            {
                var tempData = context.HttpContext.RequestServices
                    .GetRequiredService<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory>()
                    .GetTempData(context.HttpContext);
                tempData[Alerts.Error] = ex.Message;

                var referer = context.HttpContext.Request.Headers.Referer.ToString();
                context.Result = Uri.TryCreate(referer, UriKind.Absolute, out var uri)
                                 && uri.Host == context.HttpContext.Request.Host.Host
                    ? new RedirectResult(uri.PathAndQuery)
                    : new RedirectToActionResult("Index", "Home", null);
            }
            context.ExceptionHandled = true;
        }
    }

    public static class Alerts
    {
        public const string Success = "AlertSuccess";
        public const string Error = "AlertError";
    }

    public static class HttpRequestExtensions
    {
        public static bool IsAjax(this HttpRequest request) =>
            request.Headers.XRequestedWith == "XMLHttpRequest"
            || request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }
}
