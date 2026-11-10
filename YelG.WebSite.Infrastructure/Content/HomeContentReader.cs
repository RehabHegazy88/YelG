using Microsoft.EntityFrameworkCore;
using YelG.WebSite.Application.Content;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Sections;
using YelG.WebSite.Domain.Settings;
using YelG.WebSite.Infrastructure.Data;

namespace YelG.WebSite.Infrastructure.Content
{
    /// <summary>
    /// Reads the homepage with one small query per table (no tracking, collections loaded with their parents),
    /// so the page costs a fixed number of round trips however much content there is.
    /// </summary>
    public sealed class HomeContentReader : IHomeContentReader
    {
        private readonly WebSiteDbContext _db;

        public HomeContentReader(WebSiteDbContext db) => _db = db;

        public async Task<HomeContent> ReadAsync(bool includeHidden, CancellationToken ct = default)
        {
            var all = includeHidden;

            var site = await _db.SiteSettings.AsNoTracking().Include(s => s.Logo).Include(s => s.OgImage)
                           .OrderBy(s => s.Id).FirstOrDefaultAsync(ct) ?? new SiteSettings();

            var services = await _db.Services.AsNoTracking()
                .Include(s => s.Category)
                .Include(s => s.Deliverables.Where(d => all || d.IsPublished).OrderBy(d => d.SortOrder).ThenBy(d => d.Id))
                .Where(s => all || (s.IsPublished && s.Category.IsPublished))
                .OrderBy(s => s.Category.SortOrder).ThenBy(s => s.SortOrder).ThenBy(s => s.Id)
                .AsSplitQuery().ToListAsync(ct);
            var categoryIds = services.Select(s => s.CategoryId).ToHashSet();
            var categories = (await Items<ServiceCategory>(all).ToListAsync(ct)).Where(c => categoryIds.Contains(c.Id)).ToList();

            return new HomeContent
            {
                Site = site,

                Hero = await Section<HeroSection>(all).Include(h => h.Image).FirstOrDefaultAsync(ct),
                HeroStats = await Items<HeroStat>(all).ToListAsync(ct),
                Marquee = await _db.MarqueeSettings.AsNoTracking().Where(m => all || m.IsPublished).OrderBy(m => m.Id).FirstOrDefaultAsync(ct),
                MarqueeItems = await Items<MarqueeItem>(all).ToListAsync(ct),

                About = await Section<AboutSection>(all).Include(a => a.Image1).Include(a => a.Image2).FirstOrDefaultAsync(ct),
                AboutValues = await Items<AboutValue>(all).ToListAsync(ct),
                Credentials = await Items<Credential>(all).ToListAsync(ct),
                Founder = await _db.FounderSections.AsNoTracking().Include(f => f.Photo).Include(f => f.Avatar)
                    .Where(f => all || f.IsPublished).OrderBy(f => f.Id).FirstOrDefaultAsync(ct),

                Services = await Section<ServicesSection>(all).FirstOrDefaultAsync(ct),
                ServiceCategories = categories,
                ServiceList = services,

                Why = await Section<WhySection>(all).Include(w => w.Image).FirstOrDefaultAsync(ct),
                WhyCards = await Items<WhyCard>(all).ToListAsync(ct),
                WhySlides = await Items<WhySlide>(all).Include(s => s.Image).ToListAsync(ct),

                How = await Section<HowSection>(all).FirstOrDefaultAsync(ct),
                HowSteps = await Items<HowStep>(all).ToListAsync(ct),

                Purpose = await Section<PurposeSection>(all).Include(p => p.Video).Include(p => p.Poster).FirstOrDefaultAsync(ct),
                PurposeGoals = await Items<PurposeGoal>(all).ToListAsync(ct),

                Engage = await Section<EngageSection>(all).FirstOrDefaultAsync(ct),
                EngagementModels = await Items<EngagementModel>(all)
                    .Include(m => m.PreselectService)
                    .Include(m => m.Bullets.Where(b => all || b.IsPublished).OrderBy(b => b.SortOrder).ThenBy(b => b.Id))
                    .AsSplitQuery().ToListAsync(ct),

                Industries = await Section<IndustriesSection>(all).FirstOrDefaultAsync(ct),
                Sectors = await Items<IndustrySector>(all).ToListAsync(ct),
                ClientCategories = await Items<ClientCategory>(all).ToListAsync(ct),
                ClientLogos = await Items<ClientLogo>(all).Include(l => l.Logo)
                    .Where(l => l.IsApprovedForWeb && l.LogoId != null).ToListAsync(ct),

                SelfCheck = await Section<SelfCheckSection>(all).FirstOrDefaultAsync(ct),
                SelfCheckQuestions = await Items<SelfCheckQuestion>(all).Include(q => q.LinkedService).ToListAsync(ct),
                SelfCheckOptions = await Items<SelfCheckOption>(all).ToListAsync(ct),
                SelfCheckBands = await Items<SelfCheckBand>(all).ToListAsync(ct),

                Faq = await Section<FaqSection>(all).FirstOrDefaultAsync(ct),
                FaqItems = await Items<FaqItem>(all).ToListAsync(ct),

                Consult = await Section<ConsultSection>(all).FirstOrDefaultAsync(ct),
                ConsultSteps = await Items<ConsultStep>(all).ToListAsync(ct),

                Insights = await Section<InsightsSection>(all).FirstOrDefaultAsync(ct),
                InsightPosts = await Items<InsightPost>(all).Include(p => p.Image).Include(p => p.Video).ToListAsync(ct),

                Careers = await Section<CareersSection>(all).Include(c => c.Image).FirstOrDefaultAsync(ct),
                CareerRoles = await Items<CareerRole>(all).Where(r => r.IsActive)
                    .Include(r => r.Tags.Where(t => all || t.IsPublished).OrderBy(t => t.SortOrder).ThenBy(t => t.Id))
                    .Include(r => r.Questions.Where(q => all || q.IsPublished).OrderBy(q => q.SortOrder).ThenBy(q => q.Id))
                    .AsSplitQuery().ToListAsync(ct),

                Portal = await _db.ClientPortalSections.AsNoTracking().Where(p => all || p.IsPublished).OrderBy(p => p.Id).FirstOrDefaultAsync(ct)
            };
        }

        private IQueryable<T> Section<T>(bool all) where T : SectionBase =>
            _db.Set<T>().AsNoTracking().Where(s => all || s.IsPublished).OrderBy(s => s.Id);

        private IQueryable<T> Items<T>(bool all) where T : ContentItem =>
            _db.Set<T>().AsNoTracking().Where(i => all || i.IsPublished).OrderBy(i => i.SortOrder).ThenBy(i => i.Id);
    }
}
