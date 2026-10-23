using Microsoft.EntityFrameworkCore;
using YelG.DAL.Access.Maps.Common;
using YelG.Model.Audit;

namespace YelG.DAL.Access.Maps.Main
{
    public class ActivityLogMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<ActivityLog>(b =>
            {
                b.ToTable("ActivityLogs");

                b.Property(x => x.UserName).HasMaxLength(256);
                b.Property(x => x.Action).HasMaxLength(60).IsRequired();
                b.Property(x => x.Details).HasMaxLength(1000);
                b.HasIndex(x => x.OccurredAt);
            });
        }
    }
}
