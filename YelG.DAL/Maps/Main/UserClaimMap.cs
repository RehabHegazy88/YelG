
using YelG.DAL.Access.Maps.Common; 
using YelG.Model.Auth;
using Microsoft.EntityFrameworkCore;

namespace YelG.DAL.Access.Maps.Main
{
    public class UserClaimMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<UserClaim>()
                .ToTable("UserClaim").HasKey(cd => cd.Id);
        }
    }
}