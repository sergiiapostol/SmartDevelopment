using Microsoft.Extensions.DependencyInjection;

namespace SmartDevelopment.Dal.MongoDb
{
    public static class MongoDbDalServicesExtensions
    {
        public static IServiceCollection AddMongoDb(this IServiceCollection services, ConnectionSettings connectionSettings)
        {
            services.AddSingleton(connectionSettings);
            services.AddSingleton<IMongoClientFactory, MongoClientFactory>();
            services.AddSingleton<IMongoDatabaseFactory, MongoDatabaseFactory>();
            services.AddSingleton<IndexesManager>();

            return services;
        }
    }
}
