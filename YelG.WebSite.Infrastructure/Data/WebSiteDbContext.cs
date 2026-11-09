using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YelG.WebSite.Domain.Audit;
using YelG.WebSite.Domain.Careers;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Enquiries;
using YelG.WebSite.Domain.Media;
using YelG.WebSite.Domain.Sections;
using YelG.WebSite.Domain.Settings;
using YelG.WebSite.Infrastructure.Identity;
using YelG.WebSite.Infrastructure.Maps;

namespace YelG.WebSite.Infrastructure.Data
{
    public class WebSiteDbContext : IdentityDbContext<AdminUser, AdminRole, long>, IDataProtectionKeyContext
    {
        public WebSiteDbContext(DbContextOptions<WebSiteDbContext> options) : base(options)
        {
        }

        // Settings, media, audit
        public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
        public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        /// <summary>ASP.NET Core Data Protection keys, so sign-in cookies and form tokens survive restarts.</summary>
        public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

        // Homepage sections
        public DbSet<HeroSection> HeroSections => Set<HeroSection>();
        public DbSet<HeroStat> HeroStats => Set<HeroStat>();
        public DbSet<MarqueeSettings> MarqueeSettings => Set<MarqueeSettings>();
        public DbSet<MarqueeItem> MarqueeItems => Set<MarqueeItem>();
        public DbSet<AboutSection> AboutSections => Set<AboutSection>();
        public DbSet<AboutValue> AboutValues => Set<AboutValue>();
        public DbSet<Credential> Credentials => Set<Credential>();
        public DbSet<FounderSection> FounderSections => Set<FounderSection>();
        public DbSet<ServicesSection> ServicesSections => Set<ServicesSection>();
        public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<ServiceDeliverable> ServiceDeliverables => Set<ServiceDeliverable>();
        public DbSet<WhySection> WhySections => Set<WhySection>();
        public DbSet<WhyCard> WhyCards => Set<WhyCard>();
        public DbSet<WhySlide> WhySlides => Set<WhySlide>();
        public DbSet<HowSection> HowSections => Set<HowSection>();
        public DbSet<HowStep> HowSteps => Set<HowStep>();
        public DbSet<PurposeSection> PurposeSections => Set<PurposeSection>();
        public DbSet<PurposeGoal> PurposeGoals => Set<PurposeGoal>();
        public DbSet<EngageSection> EngageSections => Set<EngageSection>();
        public DbSet<EngagementModel> EngagementModels => Set<EngagementModel>();
        public DbSet<EngagementBullet> EngagementBullets => Set<EngagementBullet>();
        public DbSet<IndustriesSection> IndustriesSections => Set<IndustriesSection>();
        public DbSet<IndustrySector> IndustrySectors => Set<IndustrySector>();
        public DbSet<ClientCategory> ClientCategories => Set<ClientCategory>();
        public DbSet<ClientLogo> ClientLogos => Set<ClientLogo>();
        public DbSet<SelfCheckSection> SelfCheckSections => Set<SelfCheckSection>();
        public DbSet<SelfCheckQuestion> SelfCheckQuestions => Set<SelfCheckQuestion>();
        public DbSet<SelfCheckOption> SelfCheckOptions => Set<SelfCheckOption>();
        public DbSet<SelfCheckBand> SelfCheckBands => Set<SelfCheckBand>();
        public DbSet<FaqSection> FaqSections => Set<FaqSection>();
        public DbSet<FaqItem> FaqItems => Set<FaqItem>();
        public DbSet<ConsultSection> ConsultSections => Set<ConsultSection>();
        public DbSet<ConsultStep> ConsultSteps => Set<ConsultStep>();
        public DbSet<InsightsSection> InsightsSections => Set<InsightsSection>();
        public DbSet<InsightPost> InsightPosts => Set<InsightPost>();
        public DbSet<CareersSection> CareersSections => Set<CareersSection>();
        public DbSet<CareerRole> CareerRoles => Set<CareerRole>();
        public DbSet<CareerRoleTag> CareerRoleTags => Set<CareerRoleTag>();
        public DbSet<ClientPortalSection> ClientPortalSections => Set<ClientPortalSection>();

        // Enquiries
        public DbSet<Enquiry> Enquiries => Set<Enquiry>();
        public DbSet<EnquiryNote> EnquiryNotes => Set<EnquiryNote>();

        // Job applications (from the careers page)
        public DbSet<JobApplication> JobApplications => Set<JobApplication>();
        public DbSet<JobApplicationNote> JobApplicationNotes => Set<JobApplicationNote>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Every table is mapped by an IMap class in Maps (same style as YELG Lens).
            foreach (var mapping in MappingsHelper.GetMappings())
                mapping.Visit(builder);

            foreach (var entity in builder.Model.GetEntityTypes().Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType)).ToList())
            {
                var type = builder.Entity(entity.ClrType);

                // Who added / modified a row: links to the admin users that never delete content.
                type.HasOne(typeof(AdminUser)).WithMany().HasForeignKey(nameof(BaseEntity.AddedBy_PersonID)).OnDelete(DeleteBehavior.Restrict);
                type.HasOne(typeof(AdminUser)).WithMany().HasForeignKey(nameof(BaseEntity.ModifiedBy_PersonID)).OnDelete(DeleteBehavior.Restrict);

                if (SoftDelete.Applies(entity.ClrType))
                    type.HasQueryFilter(SoftDelete.NotDeletedFilter(entity.ClrType));
            }
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Every LocalizedText property is stored as two columns of its table: Heading_Ar, Heading_En.
            configurationBuilder.ComplexProperties<LocalizedText>();
        }
    }
}
