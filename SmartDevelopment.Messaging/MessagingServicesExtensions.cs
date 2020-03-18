using Microsoft.Extensions.DependencyInjection;
using SmartDevelopment.Messaging;

namespace SmartDevelopment.AzureStorage
{
    public static class MessagingServicesExtensions
    {
        public static IServiceCollection AddChannelsInitializer(this IServiceCollection services)
        {
            services.AddSingleton<ChannelsInitializator>();

            return services;
        }
    }
}
