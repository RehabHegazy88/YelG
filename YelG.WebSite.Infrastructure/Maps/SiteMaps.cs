using Microsoft.EntityFrameworkCore;
using YelG.WebSite.Domain.Audit;
using YelG.WebSite.Domain.Careers;
using YelG.WebSite.Domain.Enquiries;
using YelG.WebSite.Domain.Media;
using YelG.WebSite.Domain.Settings;
using YelG.WebSite.Infrastructure.Identity;

namespace YelG.WebSite.Infrastructure.Maps
{
    public class AdminUserMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<AdminUser>(b =>
            {
                b.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
            });
        }
    }

    public class SiteSettingsMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<SiteSettings>(b =>
            {
                b.ToTable("SiteSettings");
                b.Property(x => x.ContactEmail).HasMaxLength(256);
                b.Property(x => x.NotificationEmail).HasMaxLength(256);
                b.Property(x => x.Phone).HasMaxLength(40);
                b.Property(x => x.WebsiteUrl).HasMaxLength(500);
                b.Property(x => x.LinkedInUrl).HasMaxLength(500);
                b.Property(x => x.CareersEmail).HasMaxLength(256);
                b.Property(x => x.DefaultLanguage).HasMaxLength(2).IsRequired();
                b.HasOne(x => x.Logo).WithMany().HasForeignKey(x => x.LogoId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(x => x.OgImage).WithMany().HasForeignKey(x => x.OgImageId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }

    public class MediaAssetMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<MediaAsset>(b =>
            {
                b.ToTable("MediaAssets");
                b.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
                b.Property(x => x.Path).HasMaxLength(500).IsRequired();
                b.Property(x => x.ThumbnailPath).HasMaxLength(500);
                b.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
                b.HasIndex(x => x.Path).IsUnique().HasFilter(Data.SoftDelete.ActiveRowsFilter);
            });
        }
    }

    public class AuditLogMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<AuditLog>(b =>
            {
                b.ToTable("AuditLogs");
                b.Property(x => x.UserName).HasMaxLength(256);
                b.Property(x => x.Action).HasMaxLength(40).IsRequired();
                b.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
                b.Property(x => x.EntityId).HasMaxLength(40);
                b.HasIndex(x => x.OccurredAt);
                b.HasIndex(x => new { x.EntityType, x.EntityId });
            });
        }
    }

    public class EnquiryMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<Enquiry>(b =>
            {
                b.ToTable("Enquiries");
                b.Property(x => x.Culture).HasMaxLength(2).IsRequired();
                b.Property(x => x.Name).HasMaxLength(150).IsRequired();
                b.Property(x => x.Email).HasMaxLength(256).IsRequired();
                b.Property(x => x.Company).HasMaxLength(200).IsRequired();
                b.Property(x => x.Phone).HasMaxLength(40);
                b.Property(x => x.ServiceLabel).HasMaxLength(200).IsRequired();
                b.Property(x => x.Message).HasMaxLength(5000).IsRequired();
                b.Property(x => x.ConsentText).HasMaxLength(1000).IsRequired();
                b.Property(x => x.NotificationError).HasMaxLength(1000);
                b.Property(x => x.IpHash).HasMaxLength(64);
                b.Property(x => x.UserAgent).HasMaxLength(500);
                b.HasIndex(x => x.Status);
                b.HasIndex(x => x.AddedDate);
                b.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<EnquiryNote>(b =>
            {
                b.ToTable("EnquiryNotes");
                b.Property(x => x.Text).HasMaxLength(4000).IsRequired();
                b.HasOne(x => x.Enquiry).WithMany(x => x.Notes).HasForeignKey(x => x.EnquiryId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }

    public class JobApplicationMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<JobApplication>(b =>
            {
                b.ToTable("JobApplications");
                b.Property(x => x.RoleTitle).HasMaxLength(200).IsRequired();
                b.Property(x => x.Culture).HasMaxLength(2).IsRequired();
                b.Property(x => x.Name).HasMaxLength(150).IsRequired();
                b.Property(x => x.Email).HasMaxLength(256).IsRequired();
                b.Property(x => x.Phone).HasMaxLength(40);
                b.Property(x => x.LinkedInUrl).HasMaxLength(300);
                b.Property(x => x.Message).HasMaxLength(3000);
                b.Property(x => x.CvFileName).HasMaxLength(260).IsRequired();
                b.Property(x => x.CvStoredPath).HasMaxLength(300).IsRequired();
                b.Property(x => x.CvContentType).HasMaxLength(100).IsRequired();
                b.Property(x => x.ConsentText).HasMaxLength(1000).IsRequired();
                b.Property(x => x.NotificationError).HasMaxLength(1000);
                b.Property(x => x.IpHash).HasMaxLength(64);
                b.Property(x => x.UserAgent).HasMaxLength(500);
                b.HasIndex(x => x.Status);
                b.HasIndex(x => x.AddedDate);
                b.HasOne(x => x.CareerRole).WithMany().HasForeignKey(x => x.CareerRoleId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<JobApplicationNote>(b =>
            {
                b.ToTable("JobApplicationNotes");
                b.Property(x => x.Text).HasMaxLength(4000).IsRequired();
                b.HasOne(x => x.JobApplication).WithMany(x => x.Notes).HasForeignKey(x => x.JobApplicationId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}