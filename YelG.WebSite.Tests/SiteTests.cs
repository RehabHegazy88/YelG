using System.Net;
using System.Text.Json;
using AngleSharp.Html.Dom;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YelG.WebSite.Areas.Admin.Editing;
using YelG.WebSite.Domain.Enquiries;
using YelG.WebSite.Infrastructure.Data;

namespace YelG.WebSite.Tests
{
    /// <summary>The site as visitors and editors use it, through HTTP: pages, the form, the admin roles, edits.</summary>
    [Collection(WebSiteDatabaseCollection.Name)]
    public sealed class SiteTests : IAsyncLifetime
    {
        private readonly SiteFactory _site = new();

        public SiteTests(WebSiteDatabase db) => _ = db;

        public async Task InitializeAsync()
        {
            if (OperatingSystem.IsWindows() && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("YELG_SKIP_LOCALDB")))
                await _site.PrepareAsync();
        }

        public async Task DisposeAsync() => await _site.DisposeAsync();

        // ---------------------------------------------------------------- public site

        [LocalDbFact]
        public async Task Both_languages_render_with_their_direction_and_alternates()
        {
            var client = _site.Browser();
            var root = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.Redirect, root.StatusCode);
            Assert.Equal("/ar", root.Headers.Location?.OriginalString);

            foreach (var (lang, dir) in new[] { ("ar", "rtl"), ("en", "ltr") })
            {
                var page = await Html.GetAsync(client, "/" + lang);
                Assert.Equal(lang, page.DocumentElement.GetAttribute("lang"));
                Assert.Equal(dir, page.DocumentElement.GetAttribute("dir"));
                Assert.Equal(3, page.QuerySelectorAll("link[rel=alternate][hreflang]").Length);
                Assert.NotNull(page.QuerySelector("#consultForm input[name=__RequestVerificationToken]"));
                Assert.Null(page.QuerySelector("meta[name=robots]"));
                Assert.Equal(9, page.QuerySelectorAll("#fService option").Length - 2);
            }
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/fr")).StatusCode);
        }

        [LocalDbFact]
        public async Task Pages_carry_the_security_headers()
        {
            var response = await _site.Browser().GetAsync("/en");
            var csp = response.Headers.GetValues("Content-Security-Policy").Single();
            Assert.Contains("script-src 'self'", csp);
            Assert.Contains("frame-ancestors 'none'", csp);
            Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
            Assert.False(response.Headers.Contains("X-Robots-Tag"));

            var admin = await _site.Browser().GetAsync("/admin/login");
            Assert.Equal("noindex, nofollow", admin.Headers.GetValues("X-Robots-Tag").Single());
        }

        [LocalDbFact]
        public async Task The_sitemap_lists_both_languages_and_robots_keeps_crawlers_out_of_the_admin()
        {
            var client = _site.Browser();
            var sitemap = await client.GetStringAsync("/sitemap.xml");
            Assert.Contains("<loc>https://localhost/ar</loc>", sitemap);
            Assert.Contains("<loc>https://localhost/en</loc>", sitemap);
            Assert.Contains("Disallow: /admin", await client.GetStringAsync("/robots.txt"));
        }

        // ---------------------------------------------------------------- consultation form

        private async Task<PostedForm> EnquiryFormAsync(HttpClient client, string lang, string email)
        {
            var form = await Html.FormAsync(client, "/" + lang, "#consultForm");
            form.Set("name", "Site Test");
            form.Set("email", email);
            form.Set("company", "Test Co");
            form.Set("service", "tax");
            form.Set("message", "Testing the whole form through the site.");
            form.Set("consent", "true");
            return form;
        }

        [LocalDbFact]
        public async Task An_enquiry_sent_from_the_page_is_saved()
        {
            var client = _site.Browser();
            var email = $"visitor-{Guid.NewGuid():N}@test.local";
            var response = await (await EnquiryFormAsync(client, "en", email)).PostAsync(client, "application/json");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("ok").GetBoolean());

            await using var scope = _site.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
            var saved = await db.Enquiries.SingleAsync(e => e.Email == email);
            Assert.Equal("en", saved.Culture);
            Assert.Equal("Tax Consultation", saved.ServiceLabel);
            db.Enquiries.Remove(saved);
            await db.SaveChangesAsync();
        }

        [LocalDbFact]
        public async Task The_form_is_refused_without_its_token_and_answers_invalid_fields_in_the_page_language()
        {
            var client = _site.Browser();
            var form = await EnquiryFormAsync(client, "ar", "bad");
            form.Set("message", "short");

            var invalid = await form.PostAsync(client, "application/json");
            Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
            var errors = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync()).RootElement.GetProperty("errors");
            Assert.Equal("يرجى إدخال بريد إلكتروني صحيح، مثل name@company.com.", errors.GetProperty("email").GetString());
            Assert.True(errors.TryGetProperty("message", out _));

            form.Remove("__RequestVerificationToken");
            Assert.Equal(HttpStatusCode.BadRequest, (await form.PostAsync(client, "application/json")).StatusCode);
        }

        // ---------------------------------------------------------------- admin access

        [LocalDbFact]
        public async Task The_admin_panel_needs_a_sign_in_and_rejects_a_wrong_password()
        {
            var client = _site.Browser();
            var response = await client.GetAsync("/admin/sections/hero");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith("https://localhost/admin/login", response.Headers.Location!.ToString());

            var form = await Html.FormAsync(client, "/admin/login", "form");
            form.Set("Email", SiteFactory.AdminEmail);
            form.Set("Password", "not-the-password");
            var failed = await form.PostAsync(client);
            Assert.Equal(HttpStatusCode.OK, failed.StatusCode);
            Assert.Contains("البريد الإلكتروني أو كلمة المرور غير صحيحة.", await failed.Content.ReadAsStringAsync());
        }

        [LocalDbFact]
        public async Task Editors_manage_content_and_enquiries_but_not_users_settings_exports_or_deletes()
        {
            var editor = await _site.SignedInAsync(SiteFactory.EditorEmail);
            Assert.Equal(HttpStatusCode.OK, (await editor.GetAsync("/admin/sections/services")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await editor.GetAsync("/admin/media")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await editor.GetAsync("/admin/enquiries")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await editor.GetAsync("/admin/sections/settings")).StatusCode);

            foreach (var url in new[] { "/admin/users", "/admin/audit", "/admin/enquiries/export" })
            {
                var response = await editor.GetAsync(url);
                Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
                Assert.Contains("/admin/denied", response.Headers.Location!.ToString());
            }

            var admin = await _site.SignedInAsync(SiteFactory.AdminEmail);
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/admin/users")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/admin/sections/settings")).StatusCode);
            Assert.Equal("text/csv", (await admin.GetAsync("/admin/enquiries/export")).Content.Headers.ContentType?.MediaType);
        }

        [LocalDbFact]
        public async Task An_editor_cannot_delete_an_enquiry()
        {
            long id;
            await using (var scope = _site.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
                var enquiry = new Enquiry
                {
                    Name = "Keep", Email = "keep@test.local", Company = "Co", ServiceLabel = "x", Message = "kept by the role check",
                    ConsentText = "ok", ConsentAt = DateTime.UtcNow, IsNotSure = true
                };
                db.Enquiries.Add(enquiry);
                await db.SaveChangesAsync();
                id = enquiry.Id;
            }

            var editor = await _site.SignedInAsync(SiteFactory.EditorEmail);
            var page = await Html.GetAsync(editor, $"/admin/enquiries/{id}");
            Assert.Null(page.QuerySelector($"form[action='/admin/enquiries/{id}/delete']"));
            var token = ((IHtmlInputElement)page.QuerySelector("input[name=__RequestVerificationToken]")!).Value;
            var response = await editor.PostAsync($"/admin/enquiries/{id}/delete",
                new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/admin/denied", response.Headers.Location!.ToString());

            await using (var scope = _site.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
                Assert.Equal(1, await db.Enquiries.Where(e => e.Id == id).ExecuteDeleteAsync());
            }
        }

        // ---------------------------------------------------------------- every edit reaches the site

        /// <summary>
        /// For every screen: the section's own form, the first item of each list and of each child list. A text
        /// is changed through the admin form, the public page (the homepage, else the careers or news page) must
        /// show it at once, then the text is put back.
        /// </summary>
        [LocalDbFact]
        public async Task Every_admin_edit_appears_on_the_public_page_after_saving()
        {
            var admin = await _site.SignedInAsync(SiteFactory.AdminEmail);
            var registry = AdminScreens.Build();
            var checkedForms = new List<string>();
            var n = 0;

            async Task CheckAsync(string formUrl, EntityEditor editor)
            {
                var form = await Html.FormAsync(admin, formUrl);
                var field = editor.Fields.Where(f => f.IsLocalized).Select(f => f.Name + ".Ar")
                    .FirstOrDefault(name => !string.IsNullOrWhiteSpace(form.Get(name)));
                if (field is null) return;   // nothing written yet in this form (e.g. texts awaiting the company)

                var original = form.Get(field)!;
                var marker = $"QA{++n:000}X";
                form.Set(field, original + " " + marker);
                var saved = await form.PostAsync(admin);
                Assert.True(saved.StatusCode == HttpStatusCode.Redirect, $"{formUrl}: save answered {(int)saved.StatusCode}");

                // the homepage, or the site's other pages for what only they show (role tags, older posts)
                var browser = _site.Browser();
                var page = await browser.GetStringAsync("/ar");
                foreach (var other in new[] { "/ar/careers", "/ar/insights" })
                    if (!page.Contains(marker)) page = await browser.GetStringAsync(other);
                if (!page.Contains(marker))
                {
                    var preview = await admin.GetStringAsync("/admin/preview/ar");
                    Assert.Fail($"{formUrl} ({field}) did not reach the public page (in the preview: {preview.Contains(marker)}; published: {form.Get("IsPublished")}).");
                }
                checkedForms.Add(formUrl);

                var restore = await Html.FormAsync(admin, formUrl);
                restore.Set(field, original);
                Assert.Equal(HttpStatusCode.Redirect, (await restore.PostAsync(admin)).StatusCode);
            }

            foreach (var screen in registry.Screens)
            {
                var url = $"/admin/sections/{screen.Slug}";
                if (screen.Main is not null) await CheckAsync(url, screen.Main);

                var screenPage = await Html.GetAsync(admin, url);
                foreach (var list in screen.AllLists().Where(l => l.Parent is null))
                {
                    var first = screenPage.QuerySelector($"#list-{list.Path.Replace('.', '-')} tbody tr[data-id]");
                    if (first is null) continue;   // an empty list (client logos, open roles)
                    var itemUrl = $"{url}/{list.Path}/{first.GetAttribute("data-id")}";
                    await CheckAsync(itemUrl, list.Item);

                    foreach (var child in list.Children)
                    {
                        var itemPage = await Html.GetAsync(admin, itemUrl);
                        var row = itemPage.QuerySelector($"#list-{child.Path.Replace('.', '-')} tbody tr[data-id]");
                        if (row is not null) await CheckAsync($"{url}/{child.Path}/{row.GetAttribute("data-id")}", child.Item);
                    }
                }
            }

            Assert.True(checkedForms.Count >= 35, $"Only {checkedForms.Count} forms checked.");
        }
    }

    /// <summary>The consultation form's rate limit, with a small limit for the test.</summary>
    [Collection(WebSiteDatabaseCollection.Name)]
    public sealed class RateLimitTests
    {
        private sealed class StrictSite : SiteFactory
        {
            protected override int EnquiriesPer10Minutes => 2;
        }

        public RateLimitTests(WebSiteDatabase db) => _ = db;

        [LocalDbFact]
        public async Task A_device_that_sends_too_often_is_told_to_wait()
        {
            await using var site = new StrictSite();
            await site.PrepareAsync();
            var client = site.Browser();
            var form = await Html.FormAsync(client, "/en", "#consultForm");   // left empty: refused, but counted

            var statuses = new List<HttpStatusCode>();
            for (var i = 0; i < 3; i++) statuses.Add((await form.PostAsync(client, "application/json")).StatusCode);

            Assert.Equal(new[] { HttpStatusCode.UnprocessableEntity, HttpStatusCode.UnprocessableEntity, HttpStatusCode.TooManyRequests }, statuses);
            var plain = await form.PostAsync(client);
            Assert.Contains("Too many requests", await plain.Content.ReadAsStringAsync());
        }
    }
}
