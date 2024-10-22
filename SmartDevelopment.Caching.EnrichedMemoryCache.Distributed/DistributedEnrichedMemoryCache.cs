using Microsoft.Extensions.Caching.Memory;
using SmartDevelopment.Logging;
using SmartDevelopment.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartDevelopment.Caching.EnrichedMemoryCache.Distributed
{
    public class DistributedEnrichedMemoryCache : IEnrichedMemoryCache
    {
        private readonly ReleaseCacheSender _sender;
        private readonly IChannelReceiver<CacheReleaseEvent> _receiver;
        private readonly EnrichedMemoryCache _enrichedMemoryCache;
        private readonly ILogger _logger;

        public DistributedEnrichedMemoryCache(EnrichedMemoryCache enrichedMemoryCache,
            ReleaseCacheSender sender, ReleaseCacheReceiver receiver, ILogger<DistributedEnrichedMemoryCache> logger)
        {
            _sender = sender;
            _receiver = receiver;
            _enrichedMemoryCache = enrichedMemoryCache;
            _logger = logger;
        }

        public void Add<TEntity>(string key, TEntity value, MemoryCacheEntryOptions options, Dictionary<string, string> tags = null)
        {
            _enrichedMemoryCache.Add(key, value, options, tags);
        }

        public TEntity Get<TEntity>(string key)
        {
            return _enrichedMemoryCache.Get<TEntity>(key);
        }

        public IReadOnlyDictionary<string, List<string>> GetCancelationTokens()
        {
            return _enrichedMemoryCache.GetCancelationTokens();
        }

        public Task<TEntity> GetOrAdd<TEntity>(string key, Func<Task<TEntity>> valueGetter, MemoryCacheEntryOptions options, Dictionary<string, string> tags = null)
        {
            return _enrichedMemoryCache.GetOrAdd(key, valueGetter, options, tags);
        }

        public IReadOnlyDictionary<string, EnrichedMemoryCache.CacheItemUsage> GetUsage()
        {
            return _enrichedMemoryCache.GetUsage();
        }

        public async Task Remove(string key)
        {
            await _enrichedMemoryCache.Remove(key);
            try
            {
                await _sender.Add(new CacheReleaseEvent { Key = key });
            }catch(Exception ex)
            {
                _logger.Exception(ex);
            }
        }

        public async Task Remove(Dictionary<string, string> tags)
        {
            await _enrichedMemoryCache.Remove(tags);
            try
            {
                await _sender.Add(new CacheReleaseEvent { Tags = tags });
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
            }
        }
    }
}
