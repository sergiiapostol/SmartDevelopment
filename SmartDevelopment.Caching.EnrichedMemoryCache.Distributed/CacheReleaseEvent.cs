using System.Collections.Generic;

namespace SmartDevelopment.Caching.EnrichedMemoryCache.Distributed
{
    public class CacheReleaseEvent
    {
        public const string ChannelName = "CacheRelease";

        public Dictionary<string, string> Tags { get; set; }

        public string Key { get; set; }
    }
}
