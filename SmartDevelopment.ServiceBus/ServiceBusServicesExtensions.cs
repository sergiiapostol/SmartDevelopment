using Microsoft.Extensions.DependencyInjection;
using SmartDevelopment.ServiceBus;

namespace SmartDevelopment.AzureStorage
{
    public static class ServiceBusServicesExtensions
    {

        public static IServiceCollection AddServiceBusQueuesInitializer(this IServiceCollection services, ConnectionSettings connectionSettings)
        {
            services.AddSingleton(connectionSettings);
            MessagingServicesExtensions.AddChannelsInitializer(services);

            return services;
        }
    }
}
