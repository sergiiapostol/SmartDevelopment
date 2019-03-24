using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace SmartDevelopment.DependencyTracking.MongoDb
{
    internal class ProfiledMongoClientSettings
    {
        private readonly ConcurrentDictionary<int, string> _queriesBuffer = new ConcurrentDictionary<int, string>();

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
                        // ReSharper disable once SpecifyACultureInStringConversionExplicitly
                        _queriesBuffer.TryAdd(e.RequestId, e.Command.ToString());
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
                    if (_queriesBuffer.TryRemove(e.RequestId, out var query))
                    {
                        OnCommandCompleted(
                            new MongoCommandCompletedEventArgs(e.CommandName, query, true,
                                e.Duration));
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
                    if (_queriesBuffer.TryRemove(e.RequestId, out var query))
                        OnCommandCompleted(
                            new MongoCommandCompletedEventArgs(e.CommandName, query, false,
                                e.Duration));
                }
                catch
                {
                    // ignored
                }
            };
        }

        private void OnCommandCompleted(MongoCommandCompletedEventArgs args)
        {
            _dependencyProfiler.Dependency(_dependencyName, args.CommandName, args.Query, args.Success, args.Duration);
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

        private class MongoCommandCompletedEventArgs : EventArgs
        {
            public MongoCommandCompletedEventArgs(string commandName, string query, bool success, TimeSpan duration)
            {
                CommandName = commandName;
                Query = query;
                Success = success;
                Duration = duration;
            }

            public string CommandName { get; }

            public string Query { get; }

            public bool Success { get; }

            public TimeSpan Duration { get; }
        }
    }
}