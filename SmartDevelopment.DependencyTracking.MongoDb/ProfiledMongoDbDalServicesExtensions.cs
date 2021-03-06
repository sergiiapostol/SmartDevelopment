﻿using Microsoft.Extensions.DependencyInjection;
using SmartDevelopment.Dal.MongoDb;

namespace SmartDevelopment.DependencyTracking.MongoDb
{
    public static class ProfiledMongoDbDalServicesExtensions
    {
        public static IServiceCollection AddProfiledMongoDb(this IServiceCollection services, ConnectionSettings connectionSettings, ProfilingSettings profilingSettings)
        {
            services.AddMongoDb(connectionSettings);
            services.AddSingleton(profilingSettings);
            services.Remove(new ServiceDescriptor(typeof(IMongoClientFactory), typeof(MongoClientFactory), ServiceLifetime.Singleton));
            services.AddSingleton<IMongoClientFactory, ProfiledMongoClientFactory>();

            return services;
        }
    }
}
