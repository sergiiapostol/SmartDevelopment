using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;

namespace SmartDevelopment.DependencyTracking.ApplicationInsights
{
    public class DependencyStore : IDependencyStore
    {
        private readonly TelemetryClient _telemetryClient;

        public DependencyStore(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public void StoreDependency(string type, string name, string description, bool success, TimeSpan duration)
        {
            var dependency = new DependencyTelemetry
            {
                Duration = duration,
                Name = name,
                Success = success,
                Type = type,
            };

            if (!string.IsNullOrWhiteSpace(description))
                dependency.Data = description;

            _telemetryClient.TrackDependency(dependency);
        }
    }
}
