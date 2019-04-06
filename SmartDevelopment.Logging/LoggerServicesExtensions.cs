using Microsoft.Extensions.DependencyInjection;

namespace SmartDevelopment.Logging
{
    public static class LoggerServicesExtensions
    {
        public static IServiceCollection AddLogger(this IServiceCollection services, LoggerSettings settings)
        {
            services.AddSingleton(settings);
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

            return services;
        }
    }
}
