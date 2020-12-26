using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SmartDevelopment.DependencyTracking.MongoDb
{
    internal class ProfiledMongoClientSettings
    {
        private readonly ConcurrentDictionary<int, DependencyProfiler.DependencyItem> _queriesBuffer = new ConcurrentDictionary<int, DependencyProfiler.DependencyItem>();

        private readonly DependencyProfiler _dependencyProfiler;

        private readonly string _dependencyName;

        public ProfiledMongoClientSettings(DependencyProfiler dependencyProfiler, string dependencyName,
            IEnumerable<string> commandsToIgnore)
        {
            _dependencyName = dependencyName;
            _dependencyProfiler = dependencyProfiler ??
                                 throw new ArgumentNullException($"{nameof(dependencyProfiler)} is required");
            var ignoredCommands = (commandsToIgnore ?? Enumerable.Empty<string>())
                .Select(v => v.ToLower()).ToImmutableHashSet();

            OnCommandStartEvent = e =>
            {
                try
                {
                    if (e.Command != null && !ignoredCommands.Contains(e.CommandName.ToLower()))
                    {
                        var dependencyItem = _dependencyProfiler.Start(_dependencyName, e.CommandName, e.Command.ToString());
                        // ReSharper disable once SpecifyACultureInStringConversionExplicitly
                        _queriesBuffer.TryAdd(e.RequestId, dependencyItem);
                    }
                }
                catch
                {
                    // ignored
                }
            };

            OnCommandSucceededEvent = e =>
            {
                if (ignoredCommands.Contains(e.CommandName.ToLower()))
                    return;

                try
                {
                    if (_queriesBuffer.TryRemove(e.RequestId, out var dependencyItem))
                    {
                        dependencyItem.Succeed();
                        dependencyItem.Dispose();
                    }
                }
                catch
                {
                    // ignored
                }
            };

            OnCommandFailedEvent = e =>
            {
                if (ignoredCommands.Contains(e.CommandName.ToLower()))
                    return;
                try
                {
                    if (_queriesBuffer.TryRemove(e.RequestId, out var dependencyItem))
                    {
                        dependencyItem.Failed();
                        dependencyItem.Dispose();
                    }
                }
                catch
                {
                    // ignored
                }
            };
        }

        internal readonly Action<CommandStartedEvent> OnCommandStartEvent;

        internal readonly Action<CommandSucceededEvent> OnCommandSucceededEvent;

        internal readonly Action<CommandFailedEvent> OnCommandFailedEvent;

        public void ConfigureProfiling(MongoClientSettings clientSettings)
        {
            clientSettings.ClusterConfigurator += cb =>
            {
                cb.Subscribe(OnCommandStartEvent);
                cb.Subscribe(OnCommandSucceededEvent);
                cb.Subscribe(OnCommandFailedEvent);
            };
        }
    }
}