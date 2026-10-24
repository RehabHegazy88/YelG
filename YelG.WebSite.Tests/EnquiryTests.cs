using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YelG.WebSite.Application;
using YelG.WebSite.Application.Abstractions;
using YelG.WebSite.Application.Enquiries;
using YelG.WebSite.Domain.Enquiries;
using YelG.WebSite.Domain.Settings;
using YelG.WebSite.Infrastructure;
using YelG.WebSite.Infrastructure.Data;
using YelG.WebSite.Infrastructure.Email;
using YelG.WebSite.Infrastructure.Identity;
using YelG.WebSite.Infrastructure.Seeding;

namespace YelG.WebSite.Tests
{
    public class EnquiryRuleTests
    {
        private static readonly HashSet<string> Keys = new() { "acc", "treas" };

        private static EnquiryInput Valid() => new()
        {
            Name = "سارة", Email = "sara@company.com", Company = "شركة", Service = "acc",
            Message = "نريد مراجعة دفاترنا قبل نهاية العام.", Consent = true
        };

        [Fact]
        public void A_complete_form_passes() => Assert.Empty(EnquiryRules.Validate(Valid(), Keys));

        [Theory]
        [InlineData("name", "س")]
        [InlineData("email", "sara@company")]
        [InlineData("email", "sara @company.com")]
        [InlineData("email", "sara@company.com\r\nBcc: x@evil.example")]
        [InlineData("company", " ")]
        [InlineData("phone", "abc")]
        [InlineData("service", "hacked")]
        [InlineData("service", "")]
        [InlineData("message", "قصيرة جدًا")]
        public void Each_rule_matches_the_page_script(string field, string value)
        {
            var input = Valid();
            typeof(EnquiryInput).GetProperty(char.ToUpperInvariant(field[0]) + field[1..])!.SetValue(input, value);
            Assert.Equal(new[] { field }, EnquiryRules.Validate(input, Keys));
        }

        [Fact]
        public void Not_sure_is_a_valid_choice_and_the_phone_is_optional()
        {
            var input = Valid();
            input.Service = EnquiryInput.NotSure;
            input.Phone = "";
            Assert.Empty(EnquiryRules.Validate(input, Keys));
            input.Phone = "+20 10 0667 7363";
            Assert.Empty(EnquiryRules.Validate(input, Keys));
        }

        [Fact]
        public void Consent_is_required() => Assert.Equal(new[] { "consent" }, EnquiryRules.Validate(new EnquiryInput
        {
            Name = "Sara", Email = "sara@company.com", Company = "Co", Service = "acc", Message = new string('x', 20)
        }, Keys));

        [Fact]
        public void The_notification_is_bilingual_replies_to_the_visitor_and_encodes_their_text()
        {
            var mailer = new EnquiryMailer(Options.Create(new SmtpOptions { Host = "mail.test", FromAddress = "website@yelg.test" }),
                new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Site:BaseUrl"] = "https://www.example.com/" }).Build());
            var enquiry = new Enquiry
            {
                Id = 7, Name = "Sara\r\nBcc: x@evil.example", Email = "sara@company.com", Company = "Co", ServiceLabel = "إدارة الخزينة",
                Message = "<script>alert(1)</script>", ConsentAt = new DateTime(2026, 10, 24, 8, 0, 0, DateTimeKind.Utc)
            };

            using var message = mailer.Compose(enquiry, "info@yelgadvisory.com");

            Assert.DoesNotContain("\n", message.Subject ?? "");
            Assert.Equal("sara@company.com", Assert.Single(message.ReplyTo.Mailboxes).Address);
            Assert.Empty(message.Bcc);
            Assert.Contains("&lt;script&gt;", message.HtmlBody);
            Assert.DoesNotContain("<script>", message.HtmlBody);
            Assert.Contains("https://www.example.com/admin/enquiries/7", message.TextBody);
            Assert.Contains("الخدمة / Service: إدارة الخزينة", message.TextBody);
        }
    }

    /// <summary>Saving enquiries and their notification emails, against the test database.</summary>
    [Collection(WebSiteDatabaseCollection.Name)]
    public sealed class EnquiryServiceTests
    {
        public EnquiryServiceTests(WebSiteDatabase db) => _ = db;

        private sealed class RecordingQueue : IEnquiryNotificationQueue
        {
            public List<long> Ids { get; } = new();
            public void Enqueue(long enquiryId) => Ids.Add(enquiryId);
        }

        private sealed class FailingMailer : IEnquiryMailer
        {
            public Task SendAsync(Enquiry enquiry, SiteSettings site, CancellationToken ct) => throw new IOException("mail server refused the connection");
        }

        private static ServiceProvider Site(RecordingQueue? queue = null, IEnquiryMailer? mailer = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICurrentUser>(new TestUser(null));
            services.AddWebSiteApplication();
            services.AddWebSiteInfrastructure(WebSiteDatabase.ConnectionString);
            services.Configure<EnquiryOptions>(o => o.IpHashSalt = "test-salt");
            if (queue is not null) services.AddSingleton<IEnquiryNotificationQueue>(queue);
            if (mailer is not null) services.AddScoped(_ => mailer);
            services.AddIdentityCore<AdminUser>(IdentityPolicy.Configure).AddRoles<AdminRole>().AddEntityFrameworkStores<WebSiteDbContext>();
            return services.BuildServiceProvider();
        }

        private static async Task SeedAsync(IServiceProvider site)
        {
            await using var scope = site.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ContentSeeder>().SeedAsync(new SeedOptions { AdminEmail = "admin@test.local", AdminPassword = "seed-pass-123" });
        }

        private static EnquiryInput Input(string service = "treas", string? website = null) => new()
        {
            Name = "  سارة اختبار ", Email = "sara@company.com", Company = "شركة الاختبار", Phone = "+20 10 1234 5678", Service = service,
            Message = "نريد مراجعة إدارة الخزينة لدينا قبل نهاية الربع.", Consent = true, SelfCheckScore = 14, Website = website
        };

        [LocalDbFact]
        public async Task A_valid_enquiry_is_saved_with_its_consent_and_queued_for_email()
        {
            var queue = new RecordingQueue();
            await using var site = Site(queue);
            await SeedAsync(site);

            long id;
            await using (var scope = site.CreateAsyncScope())
            {
                var result = await scope.ServiceProvider.GetRequiredService<EnquiryService>()
                    .SubmitAsync(Input(), new EnquiryOrigin("ar", "203.0.113.9", "Test browser"));
                Assert.Equal(EnquiryOutcome.Saved, result.Outcome);
                id = result.EnquiryId!.Value;
            }

            await using (var scope = site.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
                var saved = await db.Enquiries.AsNoTracking().SingleAsync(e => e.Id == id);
                Assert.Equal("سارة اختبار", saved.Name);
                Assert.Equal("إدارة الخزينة", saved.ServiceLabel);
                Assert.False(saved.IsNotSure);
                Assert.Equal(14, saved.SelfCheckScore);
                Assert.Equal(EnquiryStatus.New, saved.Status);
                Assert.NotEqual(default, saved.ConsentAt);
                Assert.StartsWith("أوافق على أن تستخدم YelG Advisory", saved.ConsentText);
                Assert.Equal(64, saved.IpHash!.Length);
                Assert.DoesNotContain("203.0.113.9", saved.IpHash);
                Assert.Equal(new[] { id }, queue.Ids);
                await db.Enquiries.Where(e => e.Id == id).ExecuteDeleteAsync();
            }
        }

        [LocalDbFact]
        public async Task The_honeypot_and_invalid_forms_save_nothing()
        {
            var queue = new RecordingQueue();
            await using var site = Site(queue);
            await SeedAsync(site);
            await using var scope = site.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<EnquiryService>();
            var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
            var before = await db.Enquiries.CountAsync();

            var bot = await service.SubmitAsync(Input(website: "http://spam.example"), new EnquiryOrigin("en", null, null));
            var invalid = await service.SubmitAsync(Input(service: "unknown"), new EnquiryOrigin("en", null, null));

            Assert.Equal(EnquiryOutcome.Ignored, bot.Outcome);
            Assert.Equal(EnquiryOutcome.Invalid, invalid.Outcome);
            Assert.Equal(new[] { "service" }, invalid.Errors);
            Assert.Equal(before, await db.Enquiries.CountAsync());
            Assert.Empty(queue.Ids);
        }

        [LocalDbFact]
        public async Task When_the_mail_server_fails_the_enquiry_stays_and_the_error_is_recorded()
        {
            await using var site = Site(mailer: new FailingMailer());
            await SeedAsync(site);
            long id;
            await using (var scope = site.CreateAsyncScope())
                id = (await scope.ServiceProvider.GetRequiredService<EnquiryService>()
                    .SubmitAsync(Input(EnquiryInput.NotSure), new EnquiryOrigin("en", null, null))).EnquiryId!.Value;

            await site.GetRequiredService<EnquiryNotificationService>().SendAsync(id, CancellationToken.None);

            await using (var scope = site.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
                var saved = await db.Enquiries.AsNoTracking().SingleAsync(e => e.Id == id);
                Assert.True(saved.IsNotSure);
                Assert.StartsWith("Not sure yet", saved.ServiceLabel);
                Assert.Null(saved.NotificationSentAt);
                Assert.Equal(1, saved.NotificationAttempts);
                Assert.Contains("refused", saved.NotificationError);
                await db.Enquiries.Where(e => e.Id == id).ExecuteDeleteAsync();
            }
        }
    }
}
