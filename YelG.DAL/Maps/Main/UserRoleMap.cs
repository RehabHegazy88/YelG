
using YelG.DAL.Access.Maps.Common;
using YelG.Model.Auth;
using Microsoft.EntityFrameworkCore;

namespace YelG.DAL.Access.Maps.Main
{
    public class UserRoleMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<UserRole>()
                .ToTable("UserRoles");
            builder.Entity<UserRole>().HasKey(cd => new { cd.UserId,cd.RoleId });
            builder.Entity<UserRole>().HasOne(cd => cd.User)
                   .WithMany(cd => cd.UserRole).HasForeignKey(l => l.UserId);
            builder.Entity<UserRole>().HasOne(cd => cd.Role)
                    .WithMany(cd => cd.UserRole).HasForeignKey(l => l.RoleId);
        }
    }
}