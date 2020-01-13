using System;
using System.Diagnostics;

namespace SmartDevelopment.DependencyTracking
{
    public sealed class DependencyProfiler
    {
        private readonly DependencySettings _dependencySettings;

        private readonly IDependencyStore _dependencyStore;

        public DependencyProfiler(DependencySettings dependencySettings, IDependencyStore dependencyStore)
        {
            _dependencySettings = dependencySettings;
            _dependencyStore = dependencyStore;
        }

        public DependencyItem Start(string type, string name, string description)
        {
            return new DependencyItem(_dependencyStore, _dependencySettings).Start(type, name, description);
        }

        public class DependencyItem : DependencyTracking.DependencyItem, IDisposable
        {
            private Stopwatch _stopwatch;

            private readonly IDependencyStore _dependencyStore;

            protected readonly DependencySettings _dependencySettings;

            internal DependencyItem(IDependencyStore dependencyStore, DependencySettings dependencySettings)
            {
                _dependencyStore = dependencyStore;
                _dependencySettings = dependencySettings;
            }

            public DependencyItem Start(string type, string name, string description)
            {
                Type = type;
                Name = name;
                Description = description;
                StartTime = DateTime.UtcNow;
                _stopwatch = Stopwatch.StartNew();

                return this;
            }

            public DependencyItem Succeed(TimeSpan? duration = null)
            {
                return Complete(true, duration);
            }

            public DependencyItem Failed(TimeSpan? duration = null)
            {
                return Complete(false, duration);
            }

            private DependencyItem Complete(bool status, TimeSpan? duration)
            {
                _stopwatch.Stop();
                Duration = duration ?? _stopwatch.Elapsed;
                _stopwatch = null;
                IsSucess = status;                

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
                    Duration = _stopwatch.Elapsed;
                }

                if (_dependencySettings.Detailed == DetailsLevel.All
                    || ((_dependencySettings.Detailed & DetailsLevel.Failed) == DetailsLevel.Failed && !IsSucess)
                    || ((_dependencySettings.Detailed & DetailsLevel.Slow) == DetailsLevel.Slow
                     && Duration.TotalMilliseconds >= _dependencySettings.SlowDependencyDurationMs))
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

                _dependencyStore.StoreDependency(this);
            }
        }
    }
}