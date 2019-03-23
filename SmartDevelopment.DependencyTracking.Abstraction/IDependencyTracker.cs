using System;

namespace SmartDevelopment.DependencyTracking.Abstraction
{
    public interface IDependencyTracker
    {
        void Dependency(string type, string name, string description, bool success, TimeSpan duration);

        DependecyItem StartDependency(string type, string name, string description);
    }
}