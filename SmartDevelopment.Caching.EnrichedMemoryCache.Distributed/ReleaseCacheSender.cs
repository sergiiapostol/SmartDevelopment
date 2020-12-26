using SmartDevelopment.ServiceBus;

namespace SmartDevelopment.Caching.EnrichedMemoryCache.Distributed
{

    public class ReleaseCacheSender : BaseTopicSender<CacheReleaseEvent>
    {
        public ReleaseCacheSender(ConnectionSettings connectionSettings)
            : base(connectionSettings, CacheReleaseEvent.ChannelName)
        {
        }
    }
}
