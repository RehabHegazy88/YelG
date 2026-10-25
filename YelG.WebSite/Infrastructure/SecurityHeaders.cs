namespace YelG.WebSite.Infrastructure
{
    /// <summary>
    /// Response headers for every page: a Content Security Policy that allows only this site's scripts and
    /// styles plus Google Fonts (and YouTube's privacy-enhanced player for news videos, loaded on click), no
    /// framing of this site, no MIME sniffing, a strict referrer, no browser features the
    /// site does not use, and no indexing of the admin panel. In Development the policy is report-only, so
    /// the developer error page (inline scripts) still works.
    /// </summary>
    public static class SecurityHeaders
    {
        private const string Policy =
            "default-src 'self'; script-src 'self'; style-src 'self' https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com; img-src 'self' data: https://i.ytimg.com; media-src 'self'; connect-src 'self'; " +
            "frame-src https://www.youtube-nocookie.com; " +
            "form-action 'self'; frame-ancestors 'none'; base-uri 'self'; object-src 'none'";

        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, IWebHostEnvironment environment)
        {
            var policy = environment.IsDevelopment() ? Policy : Policy + "; upgrade-insecure-requests";
            var policyHeader = environment.IsDevelopment() ? "Content-Security-Policy-Report-Only" : "Content-Security-Policy";

            return app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    var headers = context.Response.Headers;
                    headers[policyHeader] = policy;
                    headers.XContentTypeOptions = "nosniff";
                    headers.XFrameOptions = "DENY";
                    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
                    headers["Cross-Origin-Opener-Policy"] = "same-origin";
                    if (context.Request.Path.StartsWithSegments("/admin")) headers["X-Robots-Tag"] = "noindex, nofollow";
                    return Task.CompletedTask;
                });
                await next();
            });
        }
    }
}
