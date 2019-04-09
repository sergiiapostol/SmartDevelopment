using Microsoft.Extensions.DependencyInjection;

namespace SmartDevelopment.DependencyTracking
{
    public static class DependencyProfilerServicesExtensions
    {
        public static IServiceCollection AddDependencyProfiler(this IServiceCollection services, DependencySettings dependencySettings)
        {
            services.AddSingleton(dependencySettings);
            services.AddSingleton<DependencyProfiler>();

            return services;
        }
    }
}
