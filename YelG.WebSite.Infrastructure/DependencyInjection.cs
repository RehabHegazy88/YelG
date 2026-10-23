using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YelG.WebSite.Infrastructure.Data;
using YelG.WebSite.Infrastructure.Seeding;

namespace YelG.WebSite.Infrastructure
{
    public static class DependencyInjection
    {
        /// <summary>
        /// The website database (SQL Server), its save interceptor and the seeder.
        /// The caller registers <see cref="Application.Abstractions.ICurrentUser"/> and ASP.NET Core Identity.
        /// </summary>
        public static IServiceCollection AddWebSiteInfrastructure(this IServiceCollection services, string connectionString)
        {
            services.TryAddSingleton(TimeProvider.System);
            services.AddScoped<AuditInterceptor>();
            services.AddScoped<ContentSeeder>();

            services.AddDbContext<WebSiteDbContext>((sp, options) =>
                options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(WebSiteDbContext).Assembly.FullName))
                       .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));

            return services;
        }
    }
}
