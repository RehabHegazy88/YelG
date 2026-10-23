using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YelG.DAL.Access.Helpers;
using YelG.DAL.Data;
using YelG.DAL.Interceptors;
using YelG.DAL.Seeding;

namespace YelG.DAL
{
    public static class DependencyInjection
    {
        /// <summary>Registers the DbContext. The caller must register <see cref="Abstractions.ICurrentUser"/>.</summary>
        public static IServiceCollection AddYelgData(this IServiceCollection services, string connectionString)
        {
            services.TryAddSingleton(TimeProvider.System);
            services.AddScoped<AuditableEntityInterceptor>();
            services.AddScoped<DbSeeder>();

            services.AddDbContext<YelgDbContext>((sp, options) =>
                options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(YelgDbContext).Assembly.FullName))
                       .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));

            return services;
        }

        /// <summary>
        /// Registers <see cref="IEncryptionService"/> with the key from configuration ("Encryption:Key").
        /// A missing or invalid key does not stop the app from starting; it fails with a clear message the
        /// first time something needs encryption.
        /// </summary>
        public static IServiceCollection AddYelgEncryption(this IServiceCollection services, string? base64Key)
        {
            services.AddSingleton(new EncryptionOptions { Key = base64Key });
            services.AddSingleton<IEncryptionService, EncryptionService>();
            return services;
        }
    }
}
