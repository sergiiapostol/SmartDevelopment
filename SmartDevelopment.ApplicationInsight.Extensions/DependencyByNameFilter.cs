using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;

namespace SmartDevelopment.ApplicationInsight.Extensions
{
    public class DependencyFilterSettings
    {
        public List<string> NamesToExclude { get; set; } = new List<string>();
        public List<string> CommandsToExclude { get; set; } = new List<string>();
    }
    public class DependencyByNameFilter : ITelemetryProcessor
    {
        private ITelemetryProcessor Next { get; set; }

        private DependencyFilterSettings Settings { get; set; }

        public DependencyByNameFilter(ITelemetryProcessor next, IOptions<DependencyFilterSettings> settings)
        {
            Next = next;
            Settings = settings.Value ?? new DependencyFilterSettings();
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
                    var excludedbyName = Settings.NamesToExclude.Any(v => request.Name.ToLowerInvariant().Contains(v.ToLowerInvariant()) || request.Type    .Contains(v.ToLowerInvariant()));
                    var excludedByCommand = Settings.CommandsToExclude.Any(v => request.Data.ToLowerInvariant().Contains(v.ToLowerInvariant()));
                    return !(excludedbyName || excludedByCommand) || !(request.Success ?? false);
                }
                catch { }
            }
            return true;
        }
    }
}
