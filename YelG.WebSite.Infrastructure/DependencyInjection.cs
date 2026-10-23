using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YelG.WebSite.Application.Content;
using YelG.WebSite.Infrastructure.Content;
using YelG.WebSite.Infrastructure.Data;
using YelG.WebSite.Infrastructure.Seeding;

namespace YelG.WebSite.Infrastructure
{
    public static class DependencyInjection
    {
        /// <summary>
        /// The website database (SQL Server), its save interceptor, the content readers and the seeder.
        /// The caller registers <see cref="Application.Abstractions.ICurrentUser"/>, ASP.NET Core Identity and
        /// the Application services (AddWebSiteApplication).
        /// </summary>
        public static IServiceCollection AddWebSiteInfrastructure(this IServiceCollection services, string connectionString)
        {
            services.TryAddSingleton(TimeProvider.System);
            services.AddScoped<AuditInterceptor>();
            services.AddScoped<ContentSeeder>();
            services.AddScoped<IHomeContentReader, HomeContentReader>();

            services.AddDbContext<WebSiteDbContext>((sp, options) =>
                options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(WebSiteDbContext).Assembly.FullName))
                       .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));

            return services;
        }
    }
}
