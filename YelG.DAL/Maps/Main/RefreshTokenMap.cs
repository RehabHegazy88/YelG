using Microsoft.EntityFrameworkCore;
using YelG.DAL.Access.Maps.Common;
using YelG.Model.Auth;

namespace YelG.DAL.Access.Maps.Main
{
    public class RefreshTokenMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<RefreshToken>()
                .ToTable("RefreshTokens")
                .HasKey(x => x.Id);
        }
    }
}
