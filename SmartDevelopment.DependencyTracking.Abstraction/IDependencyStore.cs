using System;
using System.Collections.Generic;

namespace SmartDevelopment.DependencyTracking
{
    public interface IDependencyStore
    {
        void StoreDependency(DependencyItem item);
    }

    public class DependencyItem
    {
        public string Name { get; internal set; }

        public string Type { get; internal set; }

        public string Description { get; internal set; }

        public bool IsSucess { get; internal set; }

        public TimeSpan Duration { get; internal set; }

        public DateTime StartTime { get; internal set; }

        public Dictionary<string, double> Metrics { get; internal set; } = new Dictionary<string, double>();

        public Dictionary<string, string> Properties { get; internal set; } = new Dictionary<string, string>();
    }
}