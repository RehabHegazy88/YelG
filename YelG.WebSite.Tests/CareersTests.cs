using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AngleSharp.Html.Dom;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YelG.WebSite.Application.Careers;
using YelG.WebSite.Domain.Careers;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Sections;
using YelG.WebSite.Infrastructure.Data;

namespace YelG.WebSite.Tests
{
    public class CareerRuleTests
    {
        [Theory]
        [InlineData("%PDF-1.7", "cv.pdf", ".pdf")]
        [InlineData("PK\u0003\u0004rest", "cv.docx", ".docx")]
        [InlineData("PK\u0003\u0004rest", "cv.zip", null)]            // a zip that is not named as a Word file
        [InlineData("<html><script>", "cv.pdf", null)]               // a page renamed to .pdf
        [InlineData("MZ\u0090\u0000program", "cv.doc", null)]        // a program renamed to .doc
        public void Cvs_are_recognised_by_their_content(string head, string name, string? extension) =>
            Assert.Equal(extension, CvFileType.Detect(System.Text.Encoding.Latin1.GetBytes(head), name)?.Extension);

        [Fact]
        public async Task An_application_needs_a_name_an_email_a_cv_and_consent()
        {
            var (errors, _) = await ApplicationRules.ValidateAsync(new ApplicationInput { LinkedIn = "javascript:alert(1)" }, null, 1000);
            Assert.Equal(new[] { "name", "email", "linkedin", "consent", "cv" }, errors);

            using var big = new MemoryStream(new byte[2000]);
            var (sizeErrors, _) = await ApplicationRules.ValidateAsync(
                new ApplicationInput { Name = "Mona", Email = "mona@company.com", Consent = true }, new CvUpload(big, "cv.pdf", big.Length), 1000);
            Assert.Equal(new[] { "cvSize" }, sizeErrors);
        }

        [Fact]
        public void A_role_is_listed_until_its_closing_day()
        {
            var role = new CareerRole { IsActive = true, ClosingOn = new DateOnly(2026, 10, 25) };
            Assert.True(role.IsOpenOn(new DateOnly(2026, 10, 25)));
            Assert.False(role.IsOpenOn(new DateOnly(2026, 10, 26)));
            Assert.False(new CareerRole { IsActive = false }.IsOpenOn(new DateOnly(2026, 1, 1)));
        }
    }

    /// <summary>The careers pages and the application form through the running site.</summary>
    [Collection(WebSiteDatabaseCollection.Name)]
    public sealed class CareersSiteTests : IAsyncLifetime
    {
        private readonly SiteFactory _site = new();
        private long _roleId;

        public CareersSiteTests(WebSiteDatabase db) => _ = db;

        public async Task InitializeAsync()
        {
            if (!OperatingSystem.IsWindows() || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("YELG_SKIP_LOCALDB"))) return;
            await _site.PrepareAsync();
            await using var scope = _site.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
            var role = new CareerRole
            {
                Slug = "test-senior-accountant", Title = new LocalizedText("محاسب أول", "Senior Accountant"),
                Department = new LocalizedText("المحاسبة", "Accounting"), Summary = new LocalizedText("ملخص", "Summary"),
                Responsibilities = new LocalizedText("أولًا\nثانيًا", "First\nSecond"), IsActive = true, SortOrder = 99
            };
            db.CareerRoles.Add(role);
            await db.SaveChangesAsync();
            _roleId = role.Id;
        }

        public async Task DisposeAsync()
        {
            if (_roleId != 0)
            {
                await using var scope = _site.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
                await db.JobApplications.Where(a => a.CareerRoleId == _roleId).ExecuteDeleteAsync();
                await db.CareerRoles.IgnoreQueryFilters().Where(r => r.Id == _roleId).ExecuteDeleteAsync();
            }
            await _site.DisposeAsync();
        }

        [LocalDbFact]
        public async Task The_list_and_the_role_pages_show_open_roles_in_both_languages()
        {
            var client = _site.Browser();
            var list = await Html.GetAsync(client, "/en/careers");
            Assert.Contains(list.QuerySelectorAll(".job-link"), a => a.GetAttribute("href") == "/en/careers/test-senior-accountant");

            var page = await Html.GetAsync(client, "/ar/careers/test-senior-accountant");
            Assert.Equal("محاسب أول", page.QuerySelector("h1")!.TextContent.Trim());
            Assert.Equal(2, page.QuerySelectorAll(".job-list li").Length);
            Assert.Equal("https://localhost/en/careers/test-senior-accountant", page.QuerySelector("link[hreflang=en]")!.GetAttribute("href"));
            Assert.Equal("/en/careers/test-senior-accountant", page.QuerySelector("[data-lang-switch]")!.GetAttribute("href"));
            var posting = JsonDocument.Parse(page.QuerySelector("script[type='application/ld+json']")!.TextContent).RootElement;
            Assert.Equal("JobPosting", posting.GetProperty("@type").GetString());

            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/ar/careers/no-such-role")).StatusCode);
            Assert.Contains("/ar/careers/test-senior-accountant", await client.GetStringAsync("/sitemap.xml"));
        }

        private static async Task<HttpResponseMessage> ApplyAsync(HttpClient client, byte[] cv, string cvName, string email,
            string pageUrl = "/en/careers/test-senior-accountant", string action = "/en/careers/test-senior-accountant/apply")
        {
            var page = await Html.GetAsync(client, pageUrl);
            var token = ((IHtmlInputElement)page.QuerySelector("#applyForm input[name=__RequestVerificationToken]")!).Value;
            var form = new MultipartFormDataContent
            {
                { new StringContent(token), "__RequestVerificationToken" },
                { new StringContent("Mona Test"), "name" },
                { new StringContent(email), "email" },
                { new StringContent("true"), "consent" },
                { new StringContent("Six years in accounting."), "message" }
            };
            var file = new ByteArrayContent(cv);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            form.Add(file, "cv", cvName);
            var request = new HttpRequestMessage(HttpMethod.Post, action) { Content = form };
            request.Headers.Accept.ParseAdd("application/json");
            return await client.SendAsync(request);
        }

        [LocalDbFact]
        public async Task An_application_with_a_cv_is_saved_and_the_cv_is_only_reachable_from_the_admin_panel()
        {
            var client = _site.Browser();
            var email = $"applicant-{Guid.NewGuid():N}@test.local";
            var pdf = "%PDF-1.4\n%%EOF"u8.ToArray();

            var refused = await ApplyAsync(client, "<html>not a pdf</html>"u8.ToArray(), "cv.pdf", email);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
            Assert.True(JsonDocument.Parse(await refused.Content.ReadAsStringAsync()).RootElement.GetProperty("errors").TryGetProperty("cv", out _));

            var saved = await ApplyAsync(client, pdf, "Mona CV.pdf", email);
            Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

            long id;
            string stored;
            await using (var scope = _site.Services.CreateAsyncScope())
            {
                var application = await scope.ServiceProvider.GetRequiredService<WebSiteDbContext>().JobApplications.AsNoTracking().SingleAsync(a => a.Email == email);
                Assert.Equal("Senior Accountant", application.RoleTitle);
                Assert.Equal("Mona CV.pdf", application.CvFileName);
                Assert.Equal("application/pdf", application.CvContentType);
                Assert.Contains("CV", application.ConsentText);
                id = application.Id;
                stored = application.CvStoredPath;
            }

            // not a public file, and the admin download needs a sign-in
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/App_Data/applications/" + stored)).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync($"/admin/applications/{id}/cv")).StatusCode);

            var editor = await _site.SignedInAsync(SiteFactory.EditorEmail);
            var download = await editor.GetAsync($"/admin/applications/{id}/cv");
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal("attachment", download.Content.Headers.ContentDisposition?.DispositionType);
            Assert.Equal(pdf, await download.Content.ReadAsByteArrayAsync());

            // an Editor cannot delete it; an Admin can, and the file goes with it
            var details = await Html.GetAsync(editor, $"/admin/applications/{id}");
            var token = ((IHtmlInputElement)details.QuerySelector("input[name=__RequestVerificationToken]")!).Value;
            var post = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
            Assert.Contains("/admin/denied", (await editor.PostAsync($"/admin/applications/{id}/delete", post)).Headers.Location!.ToString());

            var admin = await _site.SignedInAsync(SiteFactory.AdminEmail);
            var adminPage = await Html.GetAsync(admin, $"/admin/applications/{id}");
            var adminToken = ((IHtmlInputElement)adminPage.QuerySelector("input[name=__RequestVerificationToken]")!).Value;
            var deleted = await admin.PostAsync($"/admin/applications/{id}/delete",
                new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = adminToken }));
            Assert.Equal("/admin/applications", deleted.Headers.Location?.OriginalString);
            Assert.Null(_site.Services.GetRequiredService<ICvStore>().OpenRead(stored));
        }

        [LocalDbFact]
        public async Task A_general_application_is_saved_without_a_role()
        {
            var client = _site.Browser();
            var email = $"general-{Guid.NewGuid():N}@test.local";
            var response = await ApplyAsync(client, "%PDF-1.4\n%%EOF"u8.ToArray(), "cv.pdf", email, "/en/careers", "/en/careers/apply");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await using var scope = _site.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
            var application = await db.JobApplications.AsNoTracking().SingleAsync(a => a.Email == email);
            Assert.Null(application.CareerRoleId);
            Assert.Equal("General application", application.RoleTitle);
            _site.Services.GetRequiredService<ICvStore>().Delete(application.CvStoredPath);
            await db.JobApplications.Where(a => a.Id == application.Id).ExecuteDeleteAsync();
        }
    }
}