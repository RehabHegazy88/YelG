using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YelG.Model.Scope;

namespace YelG.DAL.Access.Maps.Common
{
    /// <summary>RoleHours (hours or rates per role) is an owned type stored as four columns with a suffix.</summary>
    internal static class RoleHoursMapping
    {
        public static void MapRoleHours<T>(this OwnedNavigationBuilder<T, RoleHours> rh, string suffix) where T : class
        {
            rh.Property(x => x.Analyst).HasColumnName("Analyst" + suffix);
            rh.Property(x => x.TeamLead).HasColumnName("TeamLead" + suffix);
            rh.Property(x => x.Manager).HasColumnName("Manager" + suffix);
            rh.Property(x => x.Partner).HasColumnName("Partner" + suffix);
            rh.Ignore(x => x.Total);
        }
    }
}
