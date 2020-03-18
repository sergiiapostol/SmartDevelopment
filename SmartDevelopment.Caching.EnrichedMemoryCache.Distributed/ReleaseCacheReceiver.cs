using Microsoft.Extensions.Options;
using SmartDevelopment.Logging;
using SmartDevelopment.ServiceBus;
using System.Threading.Tasks;

namespace SmartDevelopment.Caching.EnrichedMemoryCache.Distributed
{
    public class ReleaseCacheReceiver : BaseTopicReceiver<CacheReleaseEvent>
    {
        private readonly IEnrichedMemoryCache _enrichedMemoryCache;

        public ReleaseCacheReceiver(EnrichedMemoryCache enrichedMemoryCache, IOptions<DistributedEnrichedMemoryCacheSettings> settings,
            ConnectionSettings connectionSettings, ILogger<ReleaseCacheReceiver> logger)
            : base(connectionSettings, CacheReleaseEvent.ChannelName, $"cachereleaser_{settings.Value?.InstanceName ?? string.Empty}", logger)
        {
            _enrichedMemoryCache = enrichedMemoryCache;
        }

        public override Task ProcessMessage(CacheReleaseEvent message)
        { 
            return string.IsNullOrWhiteSpace(message.Key) ? _enrichedMemoryCache.Remove(message.Tags) : _enrichedMemoryCache.Remove(message.Key);
        }
    }
}
