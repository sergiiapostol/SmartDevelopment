using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static SmartDevelopment.Caching.EnrichedMemoryCache.EnrichedMemoryCache;

namespace SmartDevelopment.Caching.EnrichedMemoryCache
{
    public interface IEnrichedMemoryCache
    {
        Task<TEntity> GetOrAdd<TEntity>(string key, Func<Task<TEntity>> valueGetter, MemoryCacheEntryOptions options, Dictionary<string, string> tags = null);

        TEntity Get<TEntity>(string key);

        Task Add<TEntity>(string key, TEntity value, MemoryCacheEntryOptions options, Dictionary<string, string> tags = null);

        void Remove(string key);

        void Remove(Dictionary<string, string> tags);

        IReadOnlyDictionary<string, CacheItemUsage> GetUsage();

        IReadOnlyDictionary<string, List<string>> GetCancelationTokens();
    }
}