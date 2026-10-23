using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YelG.WebSite.Application.Content;

namespace YelG.WebSite.Application
{
    public static class DependencyInjection
    {
        /// <summary>Application services. Infrastructure provides the readers and stores they use.</summary>
        public static IServiceCollection AddWebSiteApplication(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.TryAddSingleton<IContentVersion, ContentVersion>();
            services.AddScoped<HomeContentService>();
            return services;
        }
    }
}
