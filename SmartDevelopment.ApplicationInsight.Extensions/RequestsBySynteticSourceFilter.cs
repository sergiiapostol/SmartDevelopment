using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace SmartDevelopment.ApplicationInsight.Extensions
{
    public class RequestsBySynteticSourceFilter : ITelemetryProcessor
    {
        private ITelemetryProcessor Next { get; set; }

        public RequestsBySynteticSourceFilter(ITelemetryProcessor next)
        {
            Next = next;
        }

        public void Process(ITelemetry item)
        {
            try
            {
                if (!string.IsNullOrEmpty(item.Context.Operation.SyntheticSource)) { return; }
            }
            catch { }

            Next.Process(item);
        }

    }
}
