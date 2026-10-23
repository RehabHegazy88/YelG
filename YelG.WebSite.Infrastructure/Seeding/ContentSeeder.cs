using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YelG.WebSite.Application.Common;
using YelG.WebSite.Domain.Audit;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Media;
using YelG.WebSite.Domain.Sections;
using YelG.WebSite.Domain.Settings;
using YelG.WebSite.Infrastructure.Data;
using YelG.WebSite.Infrastructure.Identity;

namespace YelG.WebSite.Infrastructure.Seeding
{
    /// <summary>Where the first Admin's credentials and the site's files come from (configuration / environment, never code).</summary>
    public sealed class SeedOptions
    {
        public string? AdminEmail { get; init; }
        public string? AdminPassword { get; init; }
        public string AdminDisplayName { get; init; } = "YelG Admin";

        /// <summary>wwwroot, to read the size of the seeded media files. Optional (tests).</summary>
        public string? WebRootPath { get; init; }
    }

    /// <summary>
    /// Loads the approved homepage content (both languages) and the first Admin. Idempotent: it only adds what
    /// is missing and never changes or restores anything an editor has edited or deleted.
    /// <list type="bullet">
    /// <item>a section row is added only while its table is empty;</item>
    /// <item>a list (FAQ, steps, …) is added only while its table is empty, deleted rows included;</item>
    /// <item>categories and services are matched by their key.</item>
    /// </list>
    /// </summary>
    public sealed class ContentSeeder
    {
        private readonly WebSiteDbContext _db;
        private readonly UserManager<AdminUser> _users;
        private readonly RoleManager<AdminRole> _roles;
        private readonly ILogger<ContentSeeder> _logger;
        private readonly Dictionary<string, int> _added = new();

        public ContentSeeder(WebSiteDbContext db, UserManager<AdminUser> users, RoleManager<AdminRole> roles, ILogger<ContentSeeder> logger)
        {
            _db = db;
            _users = users;
            _roles = roles;
            _logger = logger;
        }

        /// <summary>Counts of what this run added, by table (empty when everything already existed).</summary>
        public IReadOnlyDictionary<string, int> Added => _added;

        public async Task SeedAsync(SeedOptions options, CancellationToken ct = default)
        {
            var seed = SeedContent.Load();

            foreach (var role in AdminRoles.All)
                if (!await _roles.RoleExistsAsync(role))
                    await _roles.CreateAsync(new AdminRole(role));

            var media = await SeedMediaAsync(seed, options, ct);
            MediaAsset? M(string file) => string.IsNullOrEmpty(file) ? null : media[file];

            await SingleAsync(_db.SiteSettings, () => new SiteSettings
            {
                Tagline = seed.Site.Tagline, ContactEmail = seed.Site.ContactEmail, NotificationEmail = seed.Site.ContactEmail,
                Phone = seed.Site.Phone, WebsiteUrl = seed.Site.WebsiteUrl, LinkedInUrl = seed.Site.LinkedInUrl,
                CareersEmail = seed.Site.CareersEmail, DefaultLanguage = "ar", SeoTitle = seed.Site.SeoTitle,
                SeoDescription = seed.Site.SeoDescription, Copyright = seed.Site.Copyright, OgImage = M(seed.Hero.Image)
            }, ct);

            var h = seed.Hero;
            await SingleAsync(_db.HeroSections, () => Section(new HeroSection
            {
                PrimaryCtaLabel = h.PrimaryCtaLabel, PrimaryCtaTarget = h.PrimaryCtaTarget,
                SecondaryCtaLabel = h.SecondaryCtaLabel, SecondaryCtaTarget = h.SecondaryCtaTarget,
                SelfCheckLinkText = h.SelfCheckLinkText, Image = M(h.Image), ImageAlt = h.ImageAlt, BadgeRingText = h.BadgeRingText
            }, h), ct);
            await ListAsync(_db.HeroStats, seed.HeroStats.Select(s => new HeroStat
            {
                Kind = s.Kind, Value = s.Value, Caption = s.Caption, ScreenReaderLabel = s.ScreenReaderLabel, ChipCodes = s.ChipCodes
            }), ct);

            await SingleAsync(_db.MarqueeSettings, () => new MarqueeSettings { ScreenReaderText = seed.Marquee.ScreenReaderText }, ct);
            await ListAsync(_db.MarqueeItems, seed.Marquee.Items.Select(l => new MarqueeItem { Label = l }), ct);

            var a = seed.About;
            await SingleAsync(_db.AboutSections, () => Section(new AboutSection
            {
                CredentialsLabel = a.CredentialsLabel, Image1 = M(a.Image1), Image1Alt = a.Image1Alt,
                Image2 = M(a.Image2), Image2Alt = a.Image2Alt, BadgeRingText = a.BadgeRingText
            }, a), ct);
            await ListAsync(_db.AboutValues, seed.AboutValues.Select(v => new AboutValue { Label = v }), ct);
            await ListAsync(_db.Credentials, seed.Credentials.Select(c => new Credential { Code = c.Code, Description = c.Description }), ct);

            var f = seed.Founder;
            await SingleAsync(_db.FounderSections, () => new FounderSection
            {
                Eyebrow = f.Eyebrow, Quote = f.Quote, SupportingLine = f.SupportingLine, Name = f.Name, Title = f.Title,
                Photo = M(f.Photo), PhotoAlt = f.PhotoAlt, Avatar = M(f.Avatar)
            }, ct);

            var s0 = seed.ServicesSection;
            await SingleAsync(_db.ServicesSections, () => Section(new ServicesSection
            {
                AllTabLabel = s0.AllTabLabel, CardLinkLabel = s0.CardLinkLabel, ScopeHeading = s0.ScopeHeading, DiscussCtaLabel = s0.DiscussCtaLabel
            }, s0), ct);
            var services = await SeedServicesAsync(seed, ct);

            var w = seed.Why;
            await SingleAsync(_db.WhySections, () => Section(new WhySection
            {
                Image = M(w.Image), ImageAlt = w.ImageAlt, FloatingCardText = w.FloatingCardText
            }, w), ct);
            await ListAsync(_db.WhyCards, seed.WhyCards.Select(c => new WhyCard { Title = c.Title, Text = c.Text, IsHighlighted = c.IsHighlighted }), ct);

            await SingleAsync(_db.HowSections, () => Section(new HowSection(), seed.How), ct);
            await ListAsync(_db.HowSteps, seed.HowSteps.Select(c => new HowStep { Title = c.Title, Text = c.Text, IsHighlighted = c.IsHighlighted }), ct);

            var p = seed.Purpose;
            await SingleAsync(_db.PurposeSections, () => Section(new PurposeSection
            {
                MissionTabLabel = p.MissionTabLabel, Mission = p.Mission, VisionTabLabel = p.VisionTabLabel, Vision = p.Vision,
                GoalsTabLabel = p.GoalsTabLabel, Video = M(p.Video), Poster = M(p.Poster), VideoLabel = p.VideoLabel
            }, p), ct);
            await ListAsync(_db.PurposeGoals, seed.PurposeGoals.Select(g => new PurposeGoal { Label = g }), ct);

            await SingleAsync(_db.EngageSections, () => Section(new EngageSection(), seed.Engage), ct);
            await ListAsync(_db.EngagementModels, seed.EngagementModels.Select(m => new EngagementModel
            {
                IconKey = m.IconKey, Title = m.Title, Text = m.Text, CtaLabel = m.CtaLabel, IsHighlighted = m.IsHighlighted,
                PreselectService = m.PreselectService is null ? null : services[m.PreselectService], PreselectNotSure = m.PreselectNotSure,
                Bullets = m.Bullets.Select((b, i) => new EngagementBullet { Text = b, SortOrder = i + 1 }).ToList()
            }), ct);

            await SingleAsync(_db.IndustriesSections, () => Section(new IndustriesSection { ApprovalNote = seed.Industries.ApprovalNote }, seed.Industries), ct);
            await ListAsync(_db.IndustrySectors, seed.Sectors.Select(x => new IndustrySector { Label = x.Label, IsEmphasized = x.IsEmphasized }), ct);
            await ListAsync(_db.ClientCategories, seed.ClientCategories.Select(c => new ClientCategory
            {
                IconKey = c.IconKey, ColorVariant = c.ColorVariant, Title = c.Title, Text = c.Text
            }), ct);
            // Client logos: none. They are published only with each client's written approval.

            var q = seed.SelfCheck;
            await SingleAsync(_db.SelfCheckSections, () => Section(new SelfCheckSection
            {
                Disclaimer1 = q.Disclaimer1, Disclaimer2 = q.Disclaimer2, PickHint = q.PickHint, BackLabel = q.BackLabel,
                ResultLabel = q.ResultLabel, RecommendationsLabel = q.RecommendationsLabel, MethodNote = q.MethodNote,
                DiscussCtaLabel = q.DiscussCtaLabel, RestartLabel = q.RestartLabel, PrefillTemplate = q.PrefillTemplate
            }, q), ct);
            await ListAsync(_db.SelfCheckQuestions, seed.SelfCheckQuestions.Select(x => new SelfCheckQuestion
            {
                Area = x.Area, Text = x.Text, LinkedService = services[x.LinkedService]
            }), ct);
            await ListAsync(_db.SelfCheckOptions, seed.SelfCheckOptions.Select(o => new SelfCheckOption { Label = o.Label, Score = o.Score }), ct);
            await ListAsync(_db.SelfCheckBands, seed.SelfCheckBands.Select(b => new SelfCheckBand { MinScore = b.MinScore, Title = b.Title, Text = b.Text }), ct);

            await SingleAsync(_db.FaqSections, () => Section(new FaqSection(), seed.Faq), ct);
            await ListAsync(_db.FaqItems, seed.FaqItems.Select(x => new FaqItem { Question = x.Question, Answer = x.Answer, IsOpenByDefault = x.IsOpenByDefault }), ct);

            var c0 = seed.Consult;
            await SingleAsync(_db.ConsultSections, () => Section(new ConsultSection
            {
                DirectContactLabel = c0.DirectContactLabel, FormTitle = c0.FormTitle, RequiredNote = c0.RequiredNote,
                ConsentText = c0.ConsentText, NotSureLabel = c0.NotSureLabel, SubmitLabel = c0.SubmitLabel,
                SuccessTitle = c0.SuccessTitle, SuccessText = c0.SuccessText, FailureText = c0.FailureText
            }, c0), ct);
            await ListAsync(_db.ConsultSteps, seed.ConsultSteps.Select(x => new ConsultStep { Title = x.Title, Text = x.Text }), ct);

            var i0 = seed.Insights;
            await SingleAsync(_db.InsightsSections, () => Section(new InsightsSection { FollowLinkLabel = i0.FollowLinkLabel, FollowLinkUrl = i0.FollowLinkUrl }, i0), ct);
            await ListAsync(_db.InsightPosts, seed.InsightPosts.Select(x => new InsightPost
            {
                Image = M(x.Image), ImageAlt = x.ImageAlt, CategoryLabel = x.CategoryLabel, CategoryColor = x.CategoryColor,
                Source = x.Source, PublishedOn = x.PublishedOn, Title = x.Title, ExternalUrl = x.ExternalUrl
            }), ct);

            var k = seed.Careers;
            await SingleAsync(_db.CareersSections, () => Section(new CareersSection
            {
                Image = M(k.Image), ImageAlt = k.ImageAlt, SendCvLabel = k.SendCvLabel, CvEmailSubject = k.CvEmailSubject,
                JobsLinkLabel = k.JobsLinkLabel, JobsUrl = k.JobsUrl
            }, k), ct);
            // Open roles: none in the approved design.

            var o0 = seed.Portal;
            await SingleAsync(_db.ClientPortalSections, () => new ClientPortalSection
            {
                Status = o0.Status, BadgeLabel = o0.BadgeLabel, Title = o0.Title, Text = o0.Text, SignInLabel = o0.SignInLabel, PortalUrl = o0.PortalUrl
            }, ct);

            await _db.SaveChangesAsync(ct);
            await SeedAdminAsync(options);

            if (_added.Count > 0)
            {
                _db.AuditLogs.Add(new AuditLog
                {
                    OccurredAt = DateTime.UtcNow, Action = "Seeded", EntityType = "Seed",
                    Changes = JsonSerializer.Serialize(_added)
                });
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Website content seeded: {Tables}", string.Join(", ", _added.Select(x => $"{x.Key} {x.Value}")));
            }
        }

        private async Task<Dictionary<string, MediaAsset>> SeedMediaAsync(SeedContent seed, SeedOptions options, CancellationToken ct)
        {
            var paths = seed.Media.Select(m => "/assets/" + m).ToList();
            var existing = await _db.MediaAssets.IgnoreQueryFilters().Where(x => paths.Contains(x.Path)).ToDictionaryAsync(x => x.Path, ct);
            var result = new Dictionary<string, MediaAsset>();
            foreach (var file in seed.Media)
            {
                var path = "/assets/" + file;
                if (!existing.TryGetValue(path, out var asset))
                {
                    var onDisk = options.WebRootPath is null ? null : new FileInfo(Path.Combine(options.WebRootPath, "assets", file));
                    asset = new MediaAsset
                    {
                        Kind = file.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ? MediaKind.Video : MediaKind.Image,
                        OriginalFileName = file, Path = path, ContentType = ContentTypeOf(file),
                        SizeBytes = onDisk is { Exists: true } ? onDisk.Length : 0
                    };
                    _db.MediaAssets.Add(asset);
                    Count("MediaAssets");
                }
                result[file] = asset;
            }
            await _db.SaveChangesAsync(ct);
            return result;
        }

        private async Task<Dictionary<string, Service>> SeedServicesAsync(SeedContent seed, CancellationToken ct)
        {
            var categories = await _db.ServiceCategories.IgnoreQueryFilters().ToDictionaryAsync(c => c.Key, ct);
            var order = 0;
            foreach (var c in seed.ServiceCategories)
            {
                order++;
                if (categories.ContainsKey(c.Key)) continue;
                var category = new ServiceCategory { Key = c.Key, Name = c.Name, SortOrder = order };
                _db.ServiceCategories.Add(category);
                categories[c.Key] = category;
                Count("ServiceCategories");
            }

            var services = await _db.Services.IgnoreQueryFilters().ToDictionaryAsync(s => s.Key, ct);
            order = 0;
            foreach (var s in seed.Services)
            {
                order++;
                if (services.ContainsKey(s.Key)) continue;
                var service = new Service
                {
                    Key = s.Key, IconKey = s.IconKey, Category = categories[s.Category], Name = s.Name,
                    ShortDescription = s.ShortDescription, LongDescription = s.LongDescription, SortOrder = order,
                    Deliverables = s.Deliverables.Select((d, i) => new ServiceDeliverable { Text = d, SortOrder = i + 1 }).ToList()
                };
                _db.Services.Add(service);
                services[s.Key] = service;
                Count("Services");
            }
            await _db.SaveChangesAsync(ct);
            return services;
        }

        private async Task SeedAdminAsync(SeedOptions options)
        {
            if ((await _users.GetUsersInRoleAsync(AdminRoles.Admin)).Count > 0) return;
            if (string.IsNullOrWhiteSpace(options.AdminEmail) || string.IsNullOrWhiteSpace(options.AdminPassword))
            {
                _logger.LogWarning("No website Admin exists. Set Seed:AdminEmail and Seed:AdminPassword (user-secrets or environment variables) and restart.");
                return;
            }

            var admin = new AdminUser
            {
                UserName = options.AdminEmail, Email = options.AdminEmail, EmailConfirmed = true,
                DisplayName = options.AdminDisplayName, IsActive = true, MustChangePassword = true
            };
            var created = await _users.CreateAsync(admin, options.AdminPassword);
            if (!created.Succeeded)
            {
                _logger.LogError("Could not create the website Admin: {Errors}", string.Join("; ", created.Errors.Select(e => e.Description)));
                return;
            }
            await _users.AddToRoleAsync(admin, AdminRoles.Admin);
            Count("AdminUsers");
            _logger.LogInformation("Website Admin '{Email}' created; the password must be changed at first sign-in.", options.AdminEmail);
        }

        private static T Section<T>(T section, SectionSeed seed) where T : SectionBase
        {
            section.Eyebrow = seed.Eyebrow;
            section.Heading = seed.Heading;
            section.HideHeadingBreakOnMobile = seed.HideHeadingBreakOnMobile;
            section.Lead = seed.Lead;
            return section;
        }

        private async Task SingleAsync<T>(DbSet<T> set, Func<T> create, CancellationToken ct) where T : BaseEntity
        {
            if (await set.IgnoreQueryFilters().AnyAsync(ct)) return;
            set.Add(create());
            Count(typeof(T).Name);
        }

        private async Task ListAsync<T>(DbSet<T> set, IEnumerable<T> items, CancellationToken ct) where T : ContentItem
        {
            if (await set.IgnoreQueryFilters().AnyAsync(ct)) return;
            var order = 0;
            foreach (var item in items)
            {
                item.SortOrder = ++order;
                set.Add(item);
            }
            if (order > 0) Count(typeof(T).Name, order);
        }

        private void Count(string table, int n = 1) => _added[table] = _added.GetValueOrDefault(table) + n;

        private static string ContentTypeOf(string file) => Path.GetExtension(file).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".webm" => "video/webm",
            ".mp4" => "video/mp4",
            _ => "application/octet-stream"
        };
    }
}
