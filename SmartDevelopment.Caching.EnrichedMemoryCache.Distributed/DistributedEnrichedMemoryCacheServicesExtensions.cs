using Microsoft.Extensions.DependencyInjection;
using SmartDevelopment.Messaging;

namespace SmartDevelopment.Caching.EnrichedMemoryCache.Distributed
{
    public static class DistributedEnrichedMemoryCacheServicesExtensions
    {
        public static IServiceCollection AddDistributedEnrichedMemoryCacheInitializer(this IServiceCollection services)
        {
            services.AddSingleton<IChannel, ReleaseCacheReceiver>();
            services.AddSingleton<ReleaseCacheReceiver>();
            services.AddSingleton<IChannel, ReleaseCacheSender>();
            services.AddSingleton<ReleaseCacheSender>();
            services.AddSingleton<EnrichedMemoryCache>();
            services.AddSingleton<IEnrichedMemoryCache, DistributedEnrichedMemoryCache>();

            return services;
        }
    }
}
