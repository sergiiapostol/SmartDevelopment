using System;

namespace SmartDevelopment.DependencyTracking
{
    public class DependencySettings
    {
        public DetailsLevel Detailed { get; set; }

        public int SlowDependencyDurationMs { get; set; }
    }

    [Flags]
    public enum DetailsLevel
    {
        All = 0,
        Basic = 1,
        Slow = 2,
        Failed = 4
    }
}