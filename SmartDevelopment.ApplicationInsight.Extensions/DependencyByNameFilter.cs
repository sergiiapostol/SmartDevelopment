using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;

namespace SmartDevelopment.ApplicationInsight.Extensions
{
    public class DependencyByNameFilterSettings
    {
        public List<string> NamesToExclude { get; set; } = new List<string>();
    }
    public class DependencyByNameFilter : ITelemetryProcessor
    {
        private ITelemetryProcessor Next { get; set; }

        private DependencyByNameFilterSettings Settings { get; set; }

        public DependencyByNameFilter(ITelemetryProcessor next, IOptions<DependencyByNameFilterSettings> settings)
        {
            Next = next;
            Settings = settings.Value ?? new DependencyByNameFilterSettings();
        }

        public void Process(ITelemetry item)
        {
            if (!OKtoSend(item)) { return; }

            Next.Process(item);
        }

        private bool OKtoSend(ITelemetry item)
        {
            if (item is DependencyTelemetry)
            {
                try
                {
                    var request = item as DependencyTelemetry;
                    return !(request.Success ?? false) || !Settings.NamesToExclude.Any(v =>
                        request.Name.Contains(v) ||
                        request.Type.Contains(v));
                }
                catch { }
            }
            return true;
        }
    }
}
