using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace SmartDevelopment.DependencyTracking.ApplicationInsights
{
    public class DependencyStore : IDependencyStore
    {
        private readonly TelemetryClient _telemetryClient;

        public DependencyStore(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public void StoreDependency(DependencyItem item)
        {
            var dependency = new DependencyTelemetry
            {
                Data = item.Description,
                Duration = item.Duration,
                Name = item.Name,
                Success = item.IsSucess,
                Timestamp = item.StartTime,
                Type = item.Type
            };

            if (item.Metrics?.Count > 0)
                foreach (var metric in item.Metrics)
                {
                    dependency.Metrics.Add(metric.Key, metric.Value);
                }

            if (item.Properties?.Count > 0)
                foreach (var prop in item.Properties)
                {
                    dependency.Properties.Add(prop.Key, prop.Value);
                }

            _telemetryClient.TrackDependency(dependency);
        }
    }
}
