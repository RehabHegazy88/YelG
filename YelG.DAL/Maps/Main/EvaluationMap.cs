using Microsoft.EntityFrameworkCore;
using YelG.DAL.Access.Maps.Common;
using YelG.DAL.Data;
using YelG.Model.Evaluations;

namespace YelG.DAL.Access.Maps.Main
{
    public class CompanyMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<Company>(b =>
            {
                b.ToTable("Companies");
                b.HasQueryFilter(x => !x.Deleted);   // soft delete, see SoftDelete

                b.Property(x => x.Name).HasMaxLength(150).IsRequired();
                b.HasIndex(x => new { x.Name, x.ActivityId }).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);

                b.HasOne(x => x.Activity)
                    .WithMany()
                    .HasForeignKey(x => x.ActivityId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }

    public class EvaluationMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<Evaluation>(b =>
            {
                b.ToTable("Evaluations");
                b.HasQueryFilter(x => !x.Deleted);   // soft delete, see SoftDelete

                b.Property(x => x.Note).HasMaxLength(5000);
                b.Property(x => x.RowVersion).IsRowVersion();

                // One independent evaluation per reviewer per company.
                b.HasIndex(x => new { x.CompanyId, x.ReviewerId }).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
                b.HasIndex(x => x.Status);

                b.HasOne(x => x.Company)
                    .WithMany(x => x.Evaluations)
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Reviewer)
                    .WithMany()
                    .HasForeignKey(x => x.ReviewerId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.QuestionBankVersion)
                    .WithMany()
                    .HasForeignKey(x => x.QuestionBankVersionId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.Ignore(x => x.CanReviewerEdit);
                b.Ignore(x => x.CanAdminEdit);
                b.Ignore(x => x.HasStarted);
            });
        }
    }

    public class AnswerMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<Answer>(b =>
            {
                b.ToTable("Answers");

                b.HasIndex(x => new { x.EvaluationId, x.QuestionId }).IsUnique();

                b.HasOne(x => x.Evaluation)
                    .WithMany(x => x.Answers)
                    .HasForeignKey(x => x.EvaluationId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Restrict avoids a second cascade path (version → category → question → answer).
                b.HasOne(x => x.Question)
                    .WithMany()
                    .HasForeignKey(x => x.QuestionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
