using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartDevelopment.Dal.MongoDb;
using System.Linq;

namespace SmartDevelopment.DependencyTracking.MongoDb
{
    public class ProfiledMongoClientFactory : MongoClientFactory
    {
        private readonly ProfiledMongoClientSettings _configurator;

        public ProfiledMongoClientFactory(DependencyProfiler dependencyProfiler, IOptions<ProfilingSettings> profilingSettings)
        {
            _configurator = new ProfiledMongoClientSettings(dependencyProfiler, profilingSettings.Value.DependencyName,
                profilingSettings.Value.IgnoredCommands ?? Enumerable.Empty<string>());
        }

        public override MongoClientSettings Get(MongoUrl mongoUrl)
        {
            var settings = base.Get(mongoUrl);
            _configurator.ConfigureProfiling(settings);

            return settings;
        }
    }
}