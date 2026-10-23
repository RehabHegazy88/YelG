using Microsoft.EntityFrameworkCore;
using YelG.DAL.Access.Maps.Common;
using YelG.Model.QuestionBank;

namespace YelG.DAL.Access.Maps.Main
{
    public class QuestionBankVersionMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<QuestionBankVersion>(b =>
            {
                b.ToTable("QuestionBankVersions");

                b.HasIndex(x => x.VersionNo).IsUnique();
                b.Property(x => x.ReleaseNotes).HasMaxLength(1000);
            });
        }
    }

    public class CategoryMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<Category>(b =>
            {
                b.ToTable("Categories");

                b.Property(x => x.Code).HasMaxLength(8).IsRequired();
                b.Property(x => x.Title).HasMaxLength(150).IsRequired();
                b.Property(x => x.Status).HasMaxLength(60).IsRequired();
                b.HasIndex(x => new { x.QuestionBankVersionId, x.Code }).IsUnique();

                b.HasOne(x => x.QuestionBankVersion)
                    .WithMany(x => x.Categories)
                    .HasForeignKey(x => x.QuestionBankVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }

    public class QuestionMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<Question>(b =>
            {
                b.ToTable("Questions");

                b.Property(x => x.Code).HasMaxLength(16).IsRequired();
                b.Property(x => x.Text).HasMaxLength(1000).IsRequired();
                b.Property(x => x.ConditionValue).HasMaxLength(250);
                b.HasIndex(x => new { x.CategoryId, x.Code }).IsUnique();

                b.HasOne(x => x.Category)
                    .WithMany(x => x.Questions)
                    .HasForeignKey(x => x.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.Ignore(x => x.IsNumeric);

                // Self reference cannot cascade on SQL Server.
                b.HasOne(x => x.ParentQuestion)
                    .WithMany(x => x.ChildQuestions)
                    .HasForeignKey(x => x.ParentQuestionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }

    public class QuestionOptionMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<QuestionOption>(b =>
            {
                b.ToTable("QuestionOptions");

                b.Property(x => x.Text).HasMaxLength(250).IsRequired();
                b.HasIndex(x => new { x.QuestionId, x.Text }).IsUnique();

                b.HasOne(x => x.Question)
                    .WithMany(x => x.Options)
                    .HasForeignKey(x => x.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }

    public class QuestionScoreBandMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<QuestionScoreBand>(b =>
            {
                b.HasOne(x => x.Question)
                    .WithMany(x => x.ScoreBands)
                    .HasForeignKey(x => x.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.ToTable("QuestionScoreBands", t => t.HasCheckConstraint("CK_QuestionScoreBands_Score", "[Score] >= 0 AND [Score] <= 1"));
            });
        }
    }

    public class ActivityMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<Activity>(b =>
            {
                b.ToTable("Activities");
                b.HasIndex(x => x.ArabicName).IsUnique();

                // Same list as YELG_ACTIVITIES in the Apps Script backend. Fixed dates and ids keep the
                // seed stable between migrations (LookupEntity's constructor would use "now" / a new Guid).
                var seeded = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
                Activity Seed(long id, string arabic, string english, string uniqueId) => new()
                {
                    Id = id, ArabicName = arabic, EnglishName = english, SortOrder = (int)id, IsActive = true,
                    UniqueId = Guid.Parse(uniqueId), AddedDate = seeded, LastModifiedDate = seeded
                };
                b.HasData(
                    Seed(1, "توزيع وتجارة", "Distribution & Trade", "6f1d0b1e-0001-4c4b-9a51-1e9f00000001"),
                    Seed(2, "تصنيع", "Manufacturing", "6f1d0b1e-0002-4c4b-9a51-1e9f00000002"),
                    Seed(3, "خدمات", "Services", "6f1d0b1e-0003-4c4b-9a51-1e9f00000003"),
                    Seed(4, "خدمات طبية", "Medical Services", "6f1d0b1e-0004-4c4b-9a51-1e9f00000004"),
                    Seed(5, "مقاولات", "Contracting", "6f1d0b1e-0005-4c4b-9a51-1e9f00000005"),
                    Seed(6, "F&B – الأغذية والمشروبات", "Food & Beverage", "6f1d0b1e-0006-4c4b-9a51-1e9f00000006"),
                    Seed(7, "نشاط متعدد", "Multiple Activities", "6f1d0b1e-0007-4c4b-9a51-1e9f00000007"));
            });
        }
    }

    public class ActivityFactorMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<ActivityFactor>(b =>
            {
                b.ToTable("ActivityFactors");

                b.HasIndex(x => new { x.CategoryId, x.ActivityId }).IsUnique();

                b.HasOne(x => x.Category)
                    .WithMany(x => x.ActivityFactors)
                    .HasForeignKey(x => x.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Activity)
                    .WithMany()
                    .HasForeignKey(x => x.ActivityId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
