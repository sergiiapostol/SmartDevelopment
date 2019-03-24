using Microsoft.Extensions.DependencyInjection;

namespace SmartDevelopment.DependencyTracking.ApplicationInsights
{
    public static class DependencyTrackingServicesExtensions
    {
        public static IServiceCollection AddDependencyTrackingWithApplicationInsights(this IServiceCollection services)
        {
            services.AddSingleton<IDependencyStore, DependencyStore>();
            return services;
        }
    }
}
