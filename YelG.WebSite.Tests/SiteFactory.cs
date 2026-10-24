using System.Net;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YelG.WebSite.Application.Common;
using YelG.WebSite.Infrastructure.Data;
using YelG.WebSite.Infrastructure.Identity;
using YelG.WebSite.Infrastructure.Seeding;

namespace YelG.WebSite.Tests
{
    /// <summary>
    /// The whole site in memory (real middleware, controllers and views) against the LocalDB test database,
    /// never the database in appsettings.json. Seeds the content and two accounts with known test passwords.
    /// </summary>
    public class SiteFactory : WebApplicationFactory<Program>
    {
        public const string AdminEmail = "site.admin@test.local";
        public const string EditorEmail = "site.editor@test.local";
        public const string Password = "site-tests-pass-1";

        private readonly string _media = Path.Combine(Path.GetTempPath(), "yelg-site-tests-" + Guid.NewGuid().ToString("N"));

        protected virtual int EnquiriesPer10Minutes => 1000;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:WebSiteDb", WebSiteDatabase.ConnectionString);
            builder.UseSetting("Enquiries:IpHashSalt", "site-tests");
            builder.UseSetting("Media:StoragePath", _media);
            builder.UseSetting("Site:BaseUrl", "https://localhost");
            builder.UseSetting("RateLimits:EnquiriesPer10Minutes", EnquiriesPer10Minutes.ToString());
            builder.UseSetting("Smtp:Host", "");
        }

        /// <summary>Checks that the site really uses the test database, then seeds it. Call before any request.</summary>
        public async Task PrepareAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
            var connection = db.Database.GetConnectionString() ?? "";
            if (!connection.Contains("YelgWebSite_Tests", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The site under test is not using the LocalDB test database; stopping before any write.");

            await scope.ServiceProvider.GetRequiredService<ContentSeeder>().SeedAsync(new SeedOptions());
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AdminUser>>();
            foreach (var (email, role) in new[] { (AdminEmail, AdminRoles.Admin), (EditorEmail, AdminRoles.Editor) })
            {
                if (await users.FindByEmailAsync(email) is not null) continue;
                var user = new AdminUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = role, IsActive = true };
                var created = await users.CreateAsync(user, Password);
                if (!created.Succeeded) throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));
                await users.AddToRoleAsync(user, role);
            }
        }

        public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        /// <summary>A browser signed in to the admin panel.</summary>
        public async Task<HttpClient> SignedInAsync(string email)
        {
            var client = Browser();
            var form = await Html.FormAsync(client, "/admin/login", "form");
            form.Set("Email", email);
            form.Set("Password", Password);
            var response = await form.PostAsync(client);
            if (response.StatusCode != HttpStatusCode.Redirect || response.Headers.Location?.OriginalString != "/admin")
                throw new InvalidOperationException($"Sign-in failed: {(int)response.StatusCode}");
            return client;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && Directory.Exists(_media)) Directory.Delete(_media, recursive: true);
        }
    }

    /// <summary>Reads pages and their forms as a browser would.</summary>
    public static class Html
    {
        private static readonly HtmlParser Parser = new();

        public static async Task<IHtmlDocument> GetAsync(HttpClient client, string url)
        {
            var response = await client.GetAsync(url);
            if (response.StatusCode != HttpStatusCode.OK) throw new InvalidOperationException($"GET {url}: {(int)response.StatusCode}");
            return Parser.ParseDocument(await response.Content.ReadAsStringAsync());
        }

        public static IHtmlDocument Parse(string html) => Parser.ParseDocument(html);

        /// <summary>The fields a browser would send for the form (hidden fields, checked boxes, chosen options).</summary>
        public static async Task<PostedForm> FormAsync(HttpClient client, string url, string selector = "form.editor")
        {
            var document = await GetAsync(client, url);
            var form = (IHtmlFormElement?)document.QuerySelector(selector) ?? throw new InvalidOperationException($"No {selector} on {url}");
            var fields = new List<KeyValuePair<string, string>>();
            foreach (var element in form.Elements)
            {
                switch (element)
                {
                    case IHtmlInputElement input when !string.IsNullOrEmpty(input.Name) && !input.IsDisabled:
                        if (input.Type is "checkbox" or "radio" && !input.IsChecked) continue;
                        if (input.Type is "submit" or "button" or "file") continue;
                        fields.Add(new(input.Name, input.Value));
                        break;
                    case IHtmlTextAreaElement area when !string.IsNullOrEmpty(area.Name):
                        fields.Add(new(area.Name, area.Value));
                        break;
                    case IHtmlSelectElement select when !string.IsNullOrEmpty(select.Name) && !select.IsDisabled:
                        fields.Add(new(select.Name, select.Value ?? ""));
                        break;
                }
            }
            return new PostedForm(form.GetAttribute("action") ?? url, fields);
        }
    }

    public sealed class PostedForm
    {
        public PostedForm(string action, List<KeyValuePair<string, string>> fields)
        {
            Action = action;
            Fields = fields;
        }

        public string Action { get; }
        public List<KeyValuePair<string, string>> Fields { get; }

        public string? Get(string name) => Fields.FirstOrDefault(f => f.Key == name).Value;

        /// <summary>Replaces every value of the field (a checkbox's hidden "false" included).</summary>
        public void Set(string name, string value)
        {
            Fields.RemoveAll(f => f.Key == name);
            Fields.Add(new(name, value));
        }

        public void Remove(string name) => Fields.RemoveAll(f => f.Key == name);

        public Task<HttpResponseMessage> PostAsync(HttpClient client, string? accept = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, Action) { Content = new FormUrlEncodedContent(Fields) };
            if (accept is not null) request.Headers.Accept.ParseAdd(accept);
            return client.SendAsync(request);
        }
    }
}
