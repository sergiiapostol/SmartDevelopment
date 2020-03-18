using Microsoft.Extensions.DependencyInjection;

namespace SmartDevelopment.Caching.EnrichedMemoryCache
{
    public static class EnrichedMemoryCacheServicesExtensions
    {
        public static IServiceCollection AddEnrichedMemoryCacheInitializer(this IServiceCollection services)
        {
            services.AddSingleton<IEnrichedMemoryCache, EnrichedMemoryCache>();

            return services;
        }
    }
}
