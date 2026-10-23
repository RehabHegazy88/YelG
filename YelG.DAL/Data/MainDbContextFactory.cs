using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using YelG.DAL.Data;

namespace YelG.DAL.Access.Data;

/// <summary>
/// Used only by the EF tools (dotnet ef migrations ...). It takes precedence over the web app's settings,
/// so it must never point at a shared server by default: set ConnectionStrings__YelgDb to target one
/// explicitly, otherwise it uses a local LocalDB database. No passwords in code.
/// </summary>
public sealed class MainDbContextFactory : IDesignTimeDbContextFactory<YelgDbContext>
{
    public YelgDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__YelgDb")
            ?? @"Server=(localdb)\MSSQLLocalDB;Database=YelgLens;Trusted_Connection=True;TrustServerCertificate=True";

        var optionsBuilder = new DbContextOptionsBuilder<YelgDbContext>();
        optionsBuilder.UseSqlServer(cs, sql => sql.MigrationsAssembly(typeof(YelgDbContext).Assembly.FullName));
        return new YelgDbContext(optionsBuilder.Options);
    }
}
