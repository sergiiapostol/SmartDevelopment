using Microsoft.Extensions.DependencyInjection;

namespace SmartDevelopment.Dal.MongoDb
{
    public static class MongoDbDalServicesExtensions
    {
        public static IServiceCollection AddMongoDb(this IServiceCollection services)
        {
            services.AddSingleton<IMongoClientFactory, MongoClientFactory>();
            services.AddSingleton<IMongoDatabaseFactory, MongoDatabaseFactory>();

            return services;
        }
    }
}
