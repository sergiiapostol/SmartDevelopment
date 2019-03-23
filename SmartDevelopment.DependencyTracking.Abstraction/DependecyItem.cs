using System;
using System.Diagnostics;

namespace SmartDevelopment.DependencyTracking.Abstraction
{
    public class DependecyItem : IDisposable
    {
        private readonly Stopwatch _stopwatch;

        private readonly IDependencyTracker _tracker;

        public DependecyItem(IDependencyTracker tracker, string type, string name, string description)
        {
            _tracker = tracker;
            Type = type;
            Name = name;
            Description = description;
            _stopwatch = Stopwatch.StartNew();
        }

        public string Type { get; }

        public string Name { get; }

        public string Description { get; set; }

        public bool Success { get; set; }

        public TimeSpan Elapsed { get; private set; }

        public void Dispose()
        {
            _stopwatch.Stop();
            Elapsed = _stopwatch.Elapsed;
            _tracker.Dependency(Type, Name, Description, Success, _stopwatch.Elapsed);
        }
    }
}
