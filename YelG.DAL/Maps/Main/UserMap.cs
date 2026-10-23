
using YelG.DAL.Access.Maps.Common;
using YelG.Model.Auth;
using Microsoft.EntityFrameworkCore;

namespace YelG.DAL.Access.Maps.Main
{
   
    public class UserMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<User>()
                .ToTable("Users")
                .HasKey(x => x.Id);

            builder.Entity<User>()
                .HasOne(x => x.Department)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<User>()
                .HasIndex(x => x.DepartmentId);
        }
    
}

    public class DepartmentMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<Department>()
                .ToTable("Departments")
                .HasKey(x => x.Id);

            builder.Entity<Department>()
                .Property(x => x.ArabicName)
                .HasMaxLength(100);

            builder.Entity<Department>()
                .Property(x => x.EnglishName)
                .HasMaxLength(100);

            builder.Entity<Department>()
                .HasIndex(x => x.ArabicName);

            builder.Entity<Department>()
                .HasIndex(x => x.EnglishName);
        }
    }


}