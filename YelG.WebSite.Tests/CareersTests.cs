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
        public void Extra_questions_check_required_answers_options_and_length()
        {
            var questions = new List<CareerRoleQuestion>
            {
                new() { Id = 1, Kind = QuestionKind.YesNo, Text = new LocalizedText("هل لديك CMA؟", "Do you hold a CMA?") },
                new() { Id = 2, Kind = QuestionKind.MultipleChoice, Text = new LocalizedText("الأنظمة", "Systems"), Options = new LocalizedText("Odoo\nSAP\nOracle", "Odoo\nSAP\nOracle") },
                new() { Id = 3, Kind = QuestionKind.ShortText, Text = new LocalizedText("الراتب المتوقع", "Expected salary"), IsRequired = false },
                new() { Id = 4, Kind = QuestionKind.SingleChoice, Text = new LocalizedText("متى تبدأ؟", "Start"), Options = new LocalizedText("فورًا\nبعد شهر", "Now\nIn a month") }
            };
            static Dictionary<long, IReadOnlyList<string>> Posted(params (long Id, string[] Values)[] values) =>
                values.ToDictionary(v => v.Id, v => (IReadOnlyList<string>)v.Values);

            // nothing answered: the three required questions are missing, the optional one is fine
            Assert.Equal(new[] { "q1", "q2", "q4" }, AnswerRules.Check(questions, Posted()).Errors);

            // a value that is not one of the choices, two answers to a single choice, a text too long
            var (bad, _) = AnswerRules.Check(questions, Posted((1, ["maybe"]), (2, ["7"]), (3, [new string('x', 301)]), (4, ["0", "1"])));
            Assert.Equal(new[] { "q1", "q2", "q3:long", "q4" }, bad);

            // answered: one row per chosen option, kept in Arabic with the question's wording
            var (errors, rows) = AnswerRules.Check(questions, Posted((1, ["yes"]), (2, ["2", "0", "2"]), (4, ["1"])));
            Assert.Empty(errors);
            Assert.Equal(new[] { "yes", "Odoo", "Oracle", "بعد شهر" }, rows.Select(r => r.Value));
            Assert.All(rows.Where(r => r.CareerRoleQuestionId == 2), r => Assert.Equal("الأنظمة", r.Question));
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

        [LocalDbFact]
        public async Task The_news_page_lists_every_post_newest_first_and_filters_by_category()
        {
            var client = _site.Browser();
            var page = await Html.GetAsync(client, "/ar/insights");
            var links = page.QuerySelectorAll(".news-feature, .news-card").Select(a => a.GetAttribute("href")).ToList();
            await using var scope = _site.Services.CreateAsyncScope();
            var posts = await scope.ServiceProvider.GetRequiredService<WebSiteDbContext>().InsightPosts.AsNoTracking().ToListAsync();
            var published = posts.Where(p => p.IsPublished).OrderByDescending(p => p.PublishedOn).ThenBy(p => p.SortOrder).ToList();
            Assert.Equal(published.Select(p => p.ExternalUrl), links);

            var category = published[0].CategoryLabel.En;
            var filtered = await Html.GetAsync(client, "/en/insights?category=" + Uri.EscapeDataString(category));
            Assert.Equal(published.Count(p => p.CategoryLabel.En == category), filtered.QuerySelectorAll(".news-feature, .news-card").Length);
            Assert.Contains("/en/insights", await client.GetStringAsync("/sitemap.xml"));
            // the menu, the phone menu and the footer open the page too
            var home = await Html.GetAsync(client, "/ar");
            Assert.NotNull(home.QuerySelector(".mnav a[href='/ar/insights']"));
            Assert.NotNull(home.QuerySelector("#mobileNav a[href='/ar/insights']"));
            Assert.NotNull(home.QuerySelector(".foot-col a[href='/ar/insights']"));
            Assert.Null(home.QuerySelector(".mnav a[href='#insights']"));
        }

        [LocalDbFact]
        public async Task A_post_with_a_text_has_its_own_page_and_one_without_opens_where_it_was_published()
        {
            long id;
            string slug;
            await using (var scope = _site.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
                var post = await db.InsightPosts.OrderBy(p => p.Id).FirstAsync();
                (id, slug) = (post.Id, post.Slug);
                post.Summary = new LocalizedText("ملخص الخبر", "News summary");
                post.Body = new LocalizedText("الفقرة الأولى *مهمة*.\n\n## عنوان فرعي\nالفقرة الثانية.", "First paragraph, *highlighted*.\n\n## A subheading\nSecond paragraph.");
                await db.SaveChangesAsync();
            }
            try
            {
                _site.Services.GetRequiredService<YelG.WebSite.Application.Content.IContentVersion>().Bump();
                var client = _site.Browser();
                var page = await Html.GetAsync(client, $"/en/insights/{slug}");
                Assert.Equal(new[] { "First paragraph, highlighted.", "Second paragraph." },
                    page.QuerySelectorAll(".article-body p").Select(p => p.TextContent.Trim()));
                Assert.Equal("A subheading", page.QuerySelector(".article-body h2")!.TextContent.Trim());
                Assert.NotNull(page.QuerySelector(".article-body em"));
                Assert.Equal($"/en/insights/{slug}", (await Html.GetAsync(client, "/en/insights")).QuerySelector($".news-feature[href='/en/insights/{slug}'], .news-card[href='/en/insights/{slug}']")?.GetAttribute("href"));
                Assert.Contains($"/ar/insights/{slug}", await client.GetStringAsync("/sitemap.xml"));

                // another post without a text: its address sends visitors to where it was published
                await using var scope = _site.Services.CreateAsyncScope();
                var other = await scope.ServiceProvider.GetRequiredService<WebSiteDbContext>().InsightPosts.AsNoTracking()
                    .FirstAsync(p => p.Id != id && p.Body.Ar == "" && p.VideoId == null);
                var redirect = await client.GetAsync($"/ar/insights/{other.Slug}");
                Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
                Assert.Equal(other.ExternalUrl, redirect.Headers.Location!.ToString());
                Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/ar/insights/no-such-post")).StatusCode);
            }
            finally
            {
                await using var scope = _site.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
                var post = await db.InsightPosts.FirstAsync(p => p.Id == id);
                post.Summary = new LocalizedText();
                post.Body = new LocalizedText();
                await db.SaveChangesAsync();
                _site.Services.GetRequiredService<YelG.WebSite.Application.Content.IContentVersion>().Bump();
            }
        }

        [LocalDbFact]
        public async Task A_post_with_a_youtube_link_plays_it_on_its_page_only_after_a_click()
        {
            long id;
            string slug;
            await using (var scope = _site.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
                var post = await db.InsightPosts.OrderBy(p => p.Id).FirstAsync();
                (id, slug) = (post.Id, post.Slug);
                post.YouTubeUrl = "https://youtu.be/dQw4w9WgXcQ";
                await db.SaveChangesAsync();
            }
            try
            {
                _site.Services.GetRequiredService<YelG.WebSite.Application.Content.IContentVersion>().Bump();
                var client = _site.Browser();
                var response = await client.GetAsync($"/ar/insights/{slug}");
                var page = Html.Parse(await response.Content.ReadAsStringAsync());
                var box = page.QuerySelector("[data-yt]")!;
                Assert.Equal("dQw4w9WgXcQ", box.GetAttribute("data-yt"));
                Assert.Null(page.QuerySelector("iframe"));   // nothing from YouTube until play is pressed
                Assert.Equal("https://www.youtube.com/watch?v=dQw4w9WgXcQ", box.QuerySelector("a.yt-start")!.GetAttribute("href"));
                var policy = string.Join(" ", response.Headers.TryGetValues("Content-Security-Policy", out var v) ? v
                    : response.Headers.GetValues("Content-Security-Policy-Report-Only"));
                Assert.Contains("frame-src https://www.youtube-nocookie.com", policy);
            }
            finally
            {
                await using var scope = _site.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
                (await db.InsightPosts.FirstAsync(p => p.Id == id)).YouTubeUrl = null;
                await db.SaveChangesAsync();
                _site.Services.GetRequiredService<YelG.WebSite.Application.Content.IContentVersion>().Bump();
            }
        }

        private static async Task<HttpResponseMessage> ApplyAsync(HttpClient client, byte[] cv, string cvName, string email,
            string pageUrl = "/en/careers/test-senior-accountant", string action = "/en/careers/test-senior-accountant/apply",
            params (string Name, string Value)[] extra)
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
            foreach (var (name, value) in extra) form.Add(new StringContent(value), name);
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
        public async Task A_roles_extra_questions_are_asked_saved_and_filter_the_applications()
        {
            long yesNo, choice;
            await using (var scope = _site.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
                var q1 = new CareerRoleQuestion { CareerRoleId = _roleId, Kind = QuestionKind.YesNo, Text = new LocalizedText("هل لديك شهادة CMA؟", "Do you hold a CMA?"), SortOrder = 1 };
                var q2 = new CareerRoleQuestion
                {
                    CareerRoleId = _roleId, Kind = QuestionKind.MultipleChoice, Text = new LocalizedText("الأنظمة التي استخدمتها", "Systems you have used"),
                    Options = new LocalizedText("Odoo\nSAP\nOracle", "Odoo\nSAP\nOracle"), SortOrder = 2
                };
                db.CareerRoleQuestions.AddRange(q1, q2);
                await db.SaveChangesAsync();
                (yesNo, choice) = (q1.Id, q2.Id);
            }
            _site.Services.GetRequiredService<YelG.WebSite.Application.Content.IContentVersion>().Bump();

            var client = _site.Browser();
            var page = await Html.GetAsync(client, "/en/careers/test-senior-accountant");
            Assert.Equal(2, page.QuerySelectorAll($"#applyForm input[name=q{yesNo}]").Length);
            Assert.Equal(3, page.QuerySelectorAll($"#applyForm input[type=checkbox][name=q{choice}]").Length);

            var pdf = "%PDF-1.4\n%%EOF"u8.ToArray();
            var missing = await ApplyAsync(client, pdf, "cv.pdf", $"q-{Guid.NewGuid():N}@test.local");
            Assert.Equal(HttpStatusCode.UnprocessableEntity, missing.StatusCode);
            var errors = JsonDocument.Parse(await missing.Content.ReadAsStringAsync()).RootElement.GetProperty("errors");
            Assert.Equal("Please answer this question.", errors.GetProperty($"q{yesNo}").GetString());

            var withCma = $"cma-{Guid.NewGuid():N}@test.local";
            var without = $"nocma-{Guid.NewGuid():N}@test.local";
            Assert.Equal(HttpStatusCode.OK, (await ApplyAsync(client, pdf, "cv.pdf", withCma, extra: [($"q{yesNo}", "yes"), ($"q{choice}", "0"), ($"q{choice}", "1")])).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await ApplyAsync(client, pdf, "cv.pdf", without, extra: [($"q{yesNo}", "no"), ($"q{choice}", "2")])).StatusCode);

            await using (var scope = _site.Services.CreateAsyncScope())
            {
                var saved = await scope.ServiceProvider.GetRequiredService<WebSiteDbContext>().JobApplications.AsNoTracking()
                    .Include(a => a.Answers).SingleAsync(a => a.Email == withCma);
                Assert.Equal(new[] { "yes", "Odoo", "SAP" }, saved.Answers.OrderBy(a => a.SortOrder).ThenBy(a => a.Value).Select(a => a.Value));
            }

            // the inbox: with the role selected, each question filters the applications
            var editor = await _site.SignedInAsync(SiteFactory.EditorEmail);
            async Task<string> Inbox(string query) => (await Html.GetAsync(editor, $"/admin/applications?role={_roleId}&{query}")).QuerySelector("table.rows")?.TextContent ?? "";
            var cma = await Inbox($"a%5B{yesNo}%5D=yes");
            Assert.Contains("Mona Test", cma);
            Assert.Equal(1, (await Html.GetAsync(editor, $"/admin/applications?role={_roleId}&a%5B{yesNo}%5D=yes")).QuerySelectorAll("table.rows tbody tr").Length);
            Assert.Equal(1, (await Html.GetAsync(editor, $"/admin/applications?role={_roleId}&a%5B{choice}%5D=Oracle")).QuerySelectorAll("table.rows tbody tr").Length);
            Assert.Equal(2, (await Html.GetAsync(editor, $"/admin/applications?role={_roleId}")).QuerySelectorAll("table.rows tbody tr").Length);
            var filters = await Html.GetAsync(editor, $"/admin/applications?role={_roleId}");
            Assert.NotNull(filters.QuerySelector($"select[name='a[{yesNo}]']"));

            // the CVs of this test (the applications themselves go with the role in DisposeAsync)
            await using (var scope = _site.Services.CreateAsyncScope())
            {
                var stored = await scope.ServiceProvider.GetRequiredService<WebSiteDbContext>().JobApplications.AsNoTracking()
                    .Where(a => a.CareerRoleId == _roleId).Select(a => a.CvStoredPath).ToListAsync();
                stored.ForEach(_site.Services.GetRequiredService<ICvStore>().Delete);
            }
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