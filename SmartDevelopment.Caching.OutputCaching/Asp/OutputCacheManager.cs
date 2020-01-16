using Microsoft.AspNetCore.Http;
using SmartDevelopment.Caching.EnrichedMemoryCache;
using System.Collections.Generic;

namespace SmartDevelopment.Caching.OutputCaching
{
    public class OutputCacheManager
    {
        private readonly IEnrichedMemoryCache _enrichedMemoryCache;

        public OutputCacheManager(IEnrichedMemoryCache enrichedMemoryCache) 
        {
            _enrichedMemoryCache = enrichedMemoryCache;
        }

        public void TagCache(HttpContext context, Dictionary<string, string> tags)
        {
            context.Items.TryAdd(Consts.CachedObjectTags, tags);
        }

        public void ReleaseCache(Dictionary<string, string> tags)
        {
            _enrichedMemoryCache.Remove(tags);
        }
    }
}
