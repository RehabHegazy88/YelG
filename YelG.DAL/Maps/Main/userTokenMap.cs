
using YelG.DAL.Access.Maps.Common;
using YelG.Model.Auth;
using Microsoft.EntityFrameworkCore;

namespace YelG.DAL.Access.Maps.Main
{
    public class userTokenMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            // Identity's user-token table has a COMPOSITE key {UserId, LoginProvider, Name}.
            // It was mapped with UserId alone, which allowed only one token row per user and
            // made EF pass three key values to a single-key entity — so the token store threw
            // as soon as anything used it. Two-factor authentication is the first feature that
            // does (the authenticator key and the recovery codes are both stored here), so the
            // single-key mapping had to be corrected for 2FA to work at all.
            builder.Entity<userToken>(b =>
            {
                b.ToTable("userToken");
                b.HasKey(t => new { t.UserId, t.LoginProvider, t.Name });
                b.Property(t => t.LoginProvider).HasMaxLength(128);
                b.Property(t => t.Name).HasMaxLength(128);
            });
        }
    }



}
