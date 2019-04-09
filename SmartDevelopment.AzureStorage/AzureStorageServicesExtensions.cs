using Microsoft.Extensions.DependencyInjection;
using SmartDevelopment.AzureStorage.Blobs;
using SmartDevelopment.AzureStorage.Queues;

namespace SmartDevelopment.AzureStorage
{
    public static class AzureStorageServicesExtensions
    {
        public static IServiceCollection AddBlobsInitializer(this IServiceCollection services, ConnectionSettings connectionSettings)
        {
            services.AddSingleton(connectionSettings);
            services.AddSingleton<BlobsInitializator>();
            services.AddSingleton<BlobTransfer>();
            services.AddSingleton<IContentTypeResolver, ContentTypeResolver>();

            return services;
        }

        public static IServiceCollection AddQueuesInitializer(this IServiceCollection services, ConnectionSettings connectionSettings)
        {
            services.AddSingleton(connectionSettings);
            services.AddSingleton<QueuesInitializator>();

            return services;
        }
    }
}
