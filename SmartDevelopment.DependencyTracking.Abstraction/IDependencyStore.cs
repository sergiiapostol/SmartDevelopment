using System;

namespace SmartDevelopment.DependencyTracking
{
    public interface IDependencyStore
    {
        void StoreDependency(string type, string name, string description, bool success, TimeSpan duration);
    }
}