using Microsoft.EntityFrameworkCore;
using YelG.DAL.Access.Maps.Common;
using YelG.Model.Auth;

namespace YelG.DAL.Access.Maps.Main
{
    /// <summary>
    /// The built-in "system" user (Id 1) owns rows written without a signed-in user, such as the seeded
    /// question bank, so AddedBy_PersonID is always a real user. It has no password and is inactive:
    /// it can never sign in. Fixed stamps keep the seed stable between migrations.
    /// </summary>
    public class BuiltInUserMap : IMap
    {
        public void Visit(ModelBuilder builder)
        {
            builder.Entity<User>(b =>
            {
                b.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();

                var seeded = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
                b.HasData(new User
                {
                    Id = BuiltInUsers.SystemId,
                    UserName = BuiltInUsers.SystemUserName,
                    NormalizedUserName = BuiltInUsers.SystemUserName.ToUpperInvariant(),
                    DisplayName = "النظام",
                    IsBuiltIn = true,
                    IsActive = false,
                    LockoutEnabled = true,
                    SecurityStamp = "SYSTEM-USER-NO-SIGN-IN",
                    ConcurrencyStamp = "7c1f8d1e-5d5b-4f4e-9a51-000000000001",
                    AddedDate = seeded,
                    LastModifiedDate = seeded
                });
            });
        }
    }
}
