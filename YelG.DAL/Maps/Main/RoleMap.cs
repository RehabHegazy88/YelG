
using YelG.DAL.Access.Maps.Common; 
using YelG.Model.Auth;
using Microsoft.EntityFrameworkCore; 

namespace YelG.DAL.Access.Maps.Main
{
 
    public class RoleMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<Role>()
                .ToTable("Roles")
                .HasKey(x => x.Id);
        }
    }

}