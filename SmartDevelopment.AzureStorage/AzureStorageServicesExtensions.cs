using Microsoft.Extensions.DependencyInjection;
using SmartDevelopment.AzureStorage.Blobs;
using SmartDevelopment.AzureStorage.Queues;

namespace SmartDevelopment.Dal.MongoDb
{
    public static class AzureStorageServicesExtensions
    {
        public static IServiceCollection AddBlobsInitializer(this IServiceCollection services)
        {
            services.AddSingleton<BlobsInitializator>();

            return services;
        }

        public static IServiceCollection AddQueuesInitializer(this IServiceCollection services)
        {
            services.AddSingleton<QueuesInitializator>();

            return services;
        }
    }
}
