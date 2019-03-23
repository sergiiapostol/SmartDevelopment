using MongoDB.Driver;
using SmartDevelopment.DependencyTracking.Abstraction;
using System.Collections.Generic;
using System.Linq;

namespace SmartDevelopment.DependencyTracking.MongoDb
{
    public class MongoClientFactory : Dal.MongoDb.MongoClientFactory
    {
        private readonly ProfiledMongoClientSettings _configurator;

        private const string DependencyName = "MongoDb";

        public MongoClientFactory(IDependencyTracker dependencyTracker, string dependencyName = null,
            IEnumerable<string> notTrackedCommands = null)
        {
            notTrackedCommands = notTrackedCommands?.Any() == true ? notTrackedCommands : Enumerable.Empty<string>();

            _configurator = new ProfiledMongoClientSettings(dependencyTracker, dependencyName ?? DependencyName,
                notTrackedCommands);
        }

        public override MongoClientSettings Get(MongoUrl mongoUrl)
        {
            var settings = base.Get(mongoUrl);
            _configurator.ConfigureProfiling(settings);

            return settings;
        }
    }
}