using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace SmartDevelopment.ApplicationInsight.Extensions
{
    public class OutputCacheAppender : ITelemetryProcessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ITelemetryProcessor Next { get; set; }

        public OutputCacheAppender(ITelemetryProcessor next, IHttpContextAccessor httpContextAccessor)
        {
            Next = next;
            _httpContextAccessor = httpContextAccessor;
        }

        private const string _key = "OutputCache";

        public void Process(ITelemetry item)
        {
            try
            {
                if (item is RequestTelemetry)
                {
                    if (_httpContextAccessor.HttpContext.Items.TryGetValue(_key, out var fromCache) &&
                        (fromCache is bool) && (bool)fromCache)
                    {
                        var telemetry = item as RequestTelemetry;
                        telemetry.Properties.TryAdd(_key, bool.TrueString);
                    }
                }
            }
            catch { }

            Next.Process(item);
        }
    }
}
