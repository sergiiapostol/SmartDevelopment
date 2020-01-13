using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;

namespace SmartDevelopment.Caching.EnrichedMemoryCache
{
    public interface IEnrichedMemoryCache
    {
        TEntity Add<TEntity>(string key, TEntity value);

        TEntity Add<TEntity>(string key, TEntity value, Dictionary<string, string> tags);

        ICacheEntry CreateEntry(string key);

        void Remove(string key);

        bool TryGetValue(string key, out object value);
    }

    public class CachedObject<TEntity>
    {
        public TEntity Object { get; set; }

        public Dictionary<string, string> Tags { get; set; }

        public int TimeToLiveInSec { get; set; }
    }
}
