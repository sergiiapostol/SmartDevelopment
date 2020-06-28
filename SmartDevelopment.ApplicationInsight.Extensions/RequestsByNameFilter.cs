using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;

namespace SmartDevelopment.ApplicationInsight.Extensions
{
    public class RequestsByNameFilterSettings
    {
        public List<string> NamesToExclude { get; set; } = new List<string>();
    }
    public class RequestsByNameFilter : ITelemetryProcessor
    {
        private ITelemetryProcessor Next { get; set; }

        private RequestsByNameFilterSettings Settings { get; set; }

        public RequestsByNameFilter(ITelemetryProcessor next, IOptions<RequestsByNameFilterSettings> settings)
        {
            Next = next;
            Settings = settings.Value ?? new RequestsByNameFilterSettings();
        }

        public void Process(ITelemetry item)
        {
            if (!OKtoSend(item)) { return; }

            Next.Process(item);
        }

        private bool OKtoSend(ITelemetry item)
        {
            if (item is RequestTelemetry)
            {
                try
                {
                    var request = item as RequestTelemetry;
                    var excludedByName = Settings.NamesToExclude.Any(v =>
                        request.Name.ToLowerInvariant().Contains(v.ToLowerInvariant()) ||
                        request.Url.OriginalString.ToLowerInvariant().Contains(v.ToLowerInvariant()));

                    return !excludedByName || !(request.Success ?? false);
                }
                catch { }
            }
            return true;
        }
    }
}
