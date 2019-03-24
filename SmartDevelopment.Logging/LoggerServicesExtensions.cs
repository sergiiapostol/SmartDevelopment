using Microsoft.Extensions.DependencyInjection;

namespace SmartDevelopment.Logging
{
    public static class LoggerServicesExtensions
    {
        public static IServiceCollection AddLogger(this IServiceCollection services)
        {
            services.AddSingleton(typeof(ILogger), typeof(Logger<>));
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

            return services;
        }
    }
}
