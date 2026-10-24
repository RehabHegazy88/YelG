using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace YelG.WebSite.Infrastructure
{
    /// <summary>
    /// Per-address limits on the forms that can be abused: the consultation form and the admin sign-in.
    /// "RateLimits:EnquiriesPer10Minutes" and "RateLimits:SignInsPer5Minutes" change the numbers.
    /// The address is the client's (after the forwarded headers of the reverse proxy are applied).
    /// </summary>
    public static class RateLimits
    {
        public const string Enquiry = "enquiry";
        public const string SignIn = "sign-in";

        public static IServiceCollection AddSiteRateLimits(this IServiceCollection services, IConfiguration configuration)
        {
            var enquiries = configuration.GetValue("RateLimits:EnquiriesPer10Minutes", 5);
            var signIns = configuration.GetValue("RateLimits:SignInsPer5Minutes", 10);

            return services.AddRateLimiter(o =>
            {
                o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                o.AddPolicy(Enquiry, http => Window(http, enquiries, TimeSpan.FromMinutes(10)));
                o.AddPolicy(SignIn, http => Window(http, signIns, TimeSpan.FromMinutes(5)));
                o.OnRejected = async (context, ct) =>
                {
                    var http = context.HttpContext;
                    http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(RateLimits))
                        .LogWarning("Rate limit reached on {Path}", http.Request.Path);
                    // the page script shows its own message; a plain form post gets a readable page
                    if (!http.Request.Headers.Accept.Any(a => a?.Contains("application/json") == true))
                    {
                        http.Response.ContentType = "text/plain; charset=utf-8";
                        await http.Response.WriteAsync(
                            "طلبات كثيرة من هذا الجهاز. حاول مرة أخرى بعد بضع دقائق.\nToo many requests from this device. Please try again in a few minutes.", ct);
                    }
                };
            });
        }

        private static RateLimitPartition<string> Window(HttpContext http, int permits, TimeSpan window) =>
            RateLimitPartition.GetFixedWindowLimiter(http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window, QueueLimit = 0 });
    }
}
