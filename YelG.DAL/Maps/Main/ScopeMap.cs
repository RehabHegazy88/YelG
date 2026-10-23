using Microsoft.EntityFrameworkCore;
using YelG.DAL.Access.Maps.Common;
using YelG.DAL.Data;
using YelG.Model.Scope;

namespace YelG.DAL.Access.Maps.Main
{
    public class TaskTemplateMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<TaskTemplate>(b =>
            {
                b.ToTable("TaskTemplates");
                b.HasQueryFilter(x => !x.Deleted);   // soft delete, see SoftDelete

                b.Property(x => x.Code).HasMaxLength(20).IsRequired();
                b.Property(x => x.Title).HasMaxLength(600).IsRequired();
                b.Property(x => x.ServiceLine).HasMaxLength(120);
                b.Property(x => x.Evidence).HasMaxLength(2000);
                b.Property(x => x.Purpose).HasMaxLength(120);
                b.Property(x => x.SourceQuestionCode).HasMaxLength(16);
                b.Property(x => x.DriverQuestionCode).HasMaxLength(16);
                b.Property(x => x.BasisNote).HasMaxLength(500);
                b.HasIndex(x => x.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);

                b.OwnsOne(x => x.RoleHours, rh => rh.MapRoleHours("Hours"));
            });
        }
    }

    public class AnswerRuleMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<AnswerRule>(b =>
            {
                b.ToTable("AnswerRules");
                b.HasQueryFilter(x => !x.Deleted);   // soft delete, see SoftDelete

                b.Property(x => x.Code).HasMaxLength(20).IsRequired();
                b.Property(x => x.QuestionCode).HasMaxLength(16).IsRequired();
                b.Property(x => x.Value).HasMaxLength(500);
                b.Property(x => x.Recommendation).HasMaxLength(2000);
                b.Property(x => x.ConditionNote).HasMaxLength(2000);
                b.HasIndex(x => x.Code).IsUnique().HasFilter(SoftDelete.ActiveRowsFilter);
                b.HasIndex(x => x.QuestionCode);

                b.HasOne(x => x.TaskTemplate)
                    .WithMany()
                    .HasForeignKey(x => x.TaskTemplateId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.OwnsOne(x => x.RoleDelta, rh => rh.MapRoleHours("ExtraHours"));
            });
        }
    }

    public class ScopeItemMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<ScopeItem>(b =>
            {
                b.ToTable("ScopeItems");

                b.Property(x => x.EvidenceNote).HasMaxLength(2000);
                b.HasIndex(x => new { x.EvaluationId, x.TaskTemplateId, x.IsOptionalService }).IsUnique();

                b.HasOne(x => x.Evaluation)
                    .WithMany(x => x.ScopeItems)
                    .HasForeignKey(x => x.EvaluationId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.TaskTemplate)
                    .WithMany()
                    .HasForeignKey(x => x.TaskTemplateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }

    public class PricingSettingsMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<PricingSettings>(b =>
            {
                b.ToTable("PricingSettings");

                b.Property(x => x.MethodologyNote).HasMaxLength(2000);
                b.OwnsOne(x => x.HourlyRates, rh => rh.MapRoleHours("Rate"));
            });
        }
    }
}
