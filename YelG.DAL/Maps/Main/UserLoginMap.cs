
using YelG.DAL.Access.Maps.Common;
using YelG.Model.Auth;
using Microsoft.EntityFrameworkCore;

namespace YelG.DAL.Access.Maps.Main
{
    public class UserLoginMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<UserLogin>()
                .ToTable("UserLogin").HasKey(cd => new { cd.UserId, cd.LoginProvider, cd.ProviderKey });
        }
    }
}