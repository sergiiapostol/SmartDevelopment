using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;

namespace SmartDevelopment.DependencyTracking
{
    public sealed class DependencyProfiler
    {
        private readonly DependencySettings _dependencySettings;

        private readonly IDependencyStore _dependencyStore;

        public DependencyProfiler(IOptions<DependencySettings> dependencySettings, IDependencyStore dependencyStore)
        {
            _dependencySettings = dependencySettings.Value;
            _dependencyStore = dependencyStore;
        }

        public IDisposable Start(string type, string name, string description)
        {
            return new DependencyItem(_dependencyStore, _dependencySettings).Start(type, name, description);
        }

        public void Dependency(string type, string name, string description, bool success, TimeSpan duration)
        {
            new DependencyItem(_dependencyStore, _dependencySettings).Creat(type, name, description, success, duration).Dispose();
        }

        private class DependencyItem : IDisposable
        {
            private Stopwatch _stopwatch;

            private readonly IDependencyStore _dependencyStore;

            protected readonly DependencySettings _dependencySettings;

            internal DependencyItem(IDependencyStore dependencyStore, DependencySettings dependencySettings)
            {
                _dependencyStore = dependencyStore;
                _dependencySettings = dependencySettings;
            }

            public string Type { get; private set; }

            public string Name { get; private set; }

            public string Description { get; private set; }

            public bool Success { get; private set; }

            public TimeSpan Elapsed { get; private set; }

            public DependencyItem Start(string type, string name, string description)
            {
                Type = type;
                Name = name;
                Description = description;
                _stopwatch = Stopwatch.StartNew();

                return this;
            }

            public DependencyItem Succeed()
            {
                _stopwatch.Stop();
                Success = true;

                return this;
            }

            public DependencyItem Creat(string type, string name, string description, bool success, TimeSpan duration)
            {
                Type = type;
                Name = name;
                Description = description;
                Success = success;
                Elapsed = duration;

                return this;
            }

            public void Dispose()
            {
                if (_stopwatch != null)
                {
                    if (_stopwatch.IsRunning)
                    {
                        _stopwatch.Stop();
                    }
                    Elapsed = _stopwatch.Elapsed;
                }

                if (_dependencySettings.Detailed == DetailsLevel.All
                    || ((_dependencySettings.Detailed & DetailsLevel.Failed) == DetailsLevel.Failed && !Success)
                    || ((_dependencySettings.Detailed & DetailsLevel.Slow) == DetailsLevel.Slow
                     && Elapsed.TotalMilliseconds >= _dependencySettings.SlowDependencyDurationMs))
                {
                    Description = Description;
                }
                else if (!string.IsNullOrEmpty(Description))
                {
                    Description = Description.Substring(0, Math.Min(50, Description.Length - 1));
                }
                else
                {
                    Description = null;
                }

                _dependencyStore.StoreDependency(Type, Name, Description, Success, Elapsed);
            }
        }
    }
}