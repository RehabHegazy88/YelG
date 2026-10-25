using Microsoft.EntityFrameworkCore;
using YelG.WebSite.Domain.Sections;
using static YelG.WebSite.Infrastructure.Data.SoftDelete;

namespace YelG.WebSite.Infrastructure.Maps
{
    public class HeroMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<HeroSection>(b =>
            {
                b.ToTable("HeroSections");
                b.Property(x => x.PrimaryCtaTarget).HasMaxLength(200);
                b.Property(x => x.SecondaryCtaTarget).HasMaxLength(200);
                b.Property(x => x.BadgeRingText).HasMaxLength(120);
                b.HasOne(x => x.Image).WithMany().HasForeignKey(x => x.ImageId).OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<HeroStat>(b =>
            {
                b.ToTable("HeroStats");
                b.ComplexProperty(x => x.Value, v =>
                {
                    v.Property(t => t.Ar).HasMaxLength(20);
                    v.Property(t => t.En).HasMaxLength(20);
                });
                b.Property(x => x.ChipCodes).HasMaxLength(120);
            });
            builder.Entity<MarqueeSettings>(b => b.ToTable("MarqueeSettings"));
            builder.Entity<MarqueeItem>(b => b.ToTable("MarqueeItems"));
        }
    }

    public class AboutMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<AboutSection>(b =>
            {
                b.ToTable("AboutSections");
                b.Property(x => x.BadgeRingText).HasMaxLength(120);
                b.HasOne(x => x.Image1).WithMany().HasForeignKey(x => x.Image1Id).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Image2).WithMany().HasForeignKey(x => x.Image2Id).OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<AboutValue>(b => b.ToTable("AboutValues"));
            builder.Entity<Credential>(b =>
            {
                b.ToTable("Credentials");
                b.Property(x => x.Code).HasMaxLength(20).IsRequired();
            });
            builder.Entity<FounderSection>(b =>
            {
                b.ToTable("FounderSections");
                b.HasOne(x => x.Photo).WithMany().HasForeignKey(x => x.PhotoId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Avatar).WithMany().HasForeignKey(x => x.AvatarId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }

    public class ServicesMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<ServicesSection>(b => b.ToTable("ServicesSections"));
            builder.Entity<ServiceCategory>(b =>
            {
                b.ToTable("ServiceCategories");
                b.Property(x => x.Key).HasMaxLength(20).IsRequired();
                b.HasIndex(x => x.Key).IsUnique().HasFilter(ActiveRowsFilter);
            });
            builder.Entity<Service>(b =>
            {
                b.ToTable("Services");
                b.Property(x => x.Key).HasMaxLength(20).IsRequired();
                b.Property(x => x.IconKey).HasMaxLength(20).IsRequired();
                b.HasIndex(x => x.Key).IsUnique().HasFilter(ActiveRowsFilter);
                b.HasOne(x => x.Category).WithMany(x => x.Services).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<ServiceDeliverable>(b =>
            {
                b.ToTable("ServiceDeliverables");
                b.HasOne(x => x.Service).WithMany(x => x.Deliverables).HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }

    public class WhyHowPurposeMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<WhySection>(b =>
            {
                b.ToTable("WhySections");
                b.HasOne(x => x.Image).WithMany().HasForeignKey(x => x.ImageId).OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<WhyCard>(b => b.ToTable("WhyCards"));
            builder.Entity<HowSection>(b => b.ToTable("HowSections"));
            builder.Entity<HowStep>(b => b.ToTable("HowSteps"));
            builder.Entity<PurposeSection>(b =>
            {
                b.ToTable("PurposeSections");
                b.HasOne(x => x.Video).WithMany().HasForeignKey(x => x.VideoId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.Poster).WithMany().HasForeignKey(x => x.PosterId).OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<PurposeGoal>(b => b.ToTable("PurposeGoals"));
        }
    }

    public class EngageMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<EngageSection>(b => b.ToTable("EngageSections"));
            builder.Entity<EngagementModel>(b =>
            {
                b.ToTable("EngagementModels");
                b.Property(x => x.IconKey).HasMaxLength(20).IsRequired();
                b.HasOne(x => x.PreselectService).WithMany().HasForeignKey(x => x.PreselectServiceId).OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<EngagementBullet>(b =>
            {
                b.ToTable("EngagementBullets");
                b.HasOne(x => x.EngagementModel).WithMany(x => x.Bullets).HasForeignKey(x => x.EngagementModelId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }

    public class IndustriesMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<IndustriesSection>(b => b.ToTable("IndustriesSections"));
            builder.Entity<IndustrySector>(b => b.ToTable("IndustrySectors"));
            builder.Entity<ClientCategory>(b =>
            {
                b.ToTable("ClientCategories");
                b.Property(x => x.IconKey).HasMaxLength(20).IsRequired();
                b.Property(x => x.ColorVariant).HasMaxLength(20).IsRequired();
            });
            builder.Entity<ClientLogo>(b =>
            {
                b.ToTable("ClientLogos");
                b.Property(x => x.WebsiteUrl).HasMaxLength(500);
                b.HasOne(x => x.Logo).WithMany().HasForeignKey(x => x.LogoId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }

    public class SelfCheckMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<SelfCheckSection>(b => b.ToTable("SelfCheckSections"));
            builder.Entity<SelfCheckQuestion>(b =>
            {
                b.ToTable("SelfCheckQuestions");
                b.HasOne(x => x.LinkedService).WithMany().HasForeignKey(x => x.LinkedServiceId).OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<SelfCheckOption>(b => b.ToTable("SelfCheckOptions"));
            builder.Entity<SelfCheckBand>(b => b.ToTable("SelfCheckBands"));
        }
    }

    public class FaqConsultMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<FaqSection>(b => b.ToTable("FaqSections"));
            builder.Entity<FaqItem>(b => b.ToTable("FaqItems"));
            builder.Entity<ConsultSection>(b => b.ToTable("ConsultSections"));
            builder.Entity<ConsultStep>(b => b.ToTable("ConsultSteps"));
        }
    }

    public class InsightsCareersPortalMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<InsightsSection>(b =>
            {
                b.ToTable("InsightsSections");
                b.Property(x => x.FollowLinkUrl).HasMaxLength(500);
            });
            builder.Entity<InsightPost>(b =>
            {
                b.ToTable("InsightPosts");
                b.Property(x => x.CategoryColor).HasMaxLength(20).IsRequired();
                b.Property(x => x.Source).HasMaxLength(60);
                b.Property(x => x.ExternalUrl).HasMaxLength(500).IsRequired();
                b.HasOne(x => x.Image).WithMany().HasForeignKey(x => x.ImageId).OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<CareersSection>(b =>
            {
                b.ToTable("CareersSections");
                b.Property(x => x.CvEmailSubject).HasMaxLength(200);
                b.Property(x => x.JobsUrl).HasMaxLength(500);
                b.HasOne(x => x.Image).WithMany().HasForeignKey(x => x.ImageId).OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<CareerRole>(b =>
            {
                b.ToTable("CareerRoles");
                b.Property(x => x.Slug).HasMaxLength(80).IsRequired();
                b.HasIndex(x => x.Slug).IsUnique().HasFilter(ActiveRowsFilter);
                b.Property(x => x.ApplyUrl).HasMaxLength(500);
                b.Property(x => x.ApplyEmail).HasMaxLength(256);
            });
            builder.Entity<CareerRoleTag>(b =>
            {
                b.ToTable("CareerRoleTags");
                b.HasOne(x => x.CareerRole).WithMany(x => x.Tags).HasForeignKey(x => x.CareerRoleId).OnDelete(DeleteBehavior.Cascade);
            });
            builder.Entity<ClientPortalSection>(b =>
            {
                b.ToTable("ClientPortalSections");
                b.Property(x => x.PortalUrl).HasMaxLength(500);
                b.Ignore(x => x.ShowsSignIn);
            });
        }
    }
}
