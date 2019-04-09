using MongoDB.Driver;
using SmartDevelopment.Dal.MongoDb;
using System.Linq;

namespace SmartDevelopment.DependencyTracking.MongoDb
{
    public class ProfiledMongoClientFactory : MongoClientFactory
    {
        private readonly ProfiledMongoClientSettings _configurator;

        public ProfiledMongoClientFactory(DependencyProfiler dependencyProfiler, ProfilingSettings profilingSettings)
        {
            _configurator = new ProfiledMongoClientSettings(dependencyProfiler, profilingSettings.DependencyName,
                profilingSettings.IgnoredCommands ?? Enumerable.Empty<string>());
        }

        public override MongoClientSettings Get(MongoUrl mongoUrl)
        {
            var settings = base.Get(mongoUrl);
            _configurator.ConfigureProfiling(settings);

            return settings;
        }
    }
}