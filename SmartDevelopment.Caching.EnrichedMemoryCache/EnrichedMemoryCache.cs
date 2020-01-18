using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using SmartDevelopment.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartDevelopment.Caching.EnrichedMemoryCache
{
    public class EnrichedMemoryCache : IEnrichedMemoryCache, IDisposable
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger _logger;


        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, CancellationTokenSource>>> _cancelationTokens =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, CancellationTokenSource>>>();

        private readonly ConcurrentDictionary<string, CacheItemUsage> _cacheKeyUsage = new ConcurrentDictionary<string, CacheItemUsage>();

        private Timer _timer;

        public EnrichedMemoryCache(IMemoryCache memoryCache, ILogger<EnrichedMemoryCache> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _timer = new Timer(ReportUsage, this, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
        }

        public Task<TEntity> GetOrAdd<TEntity>(string key, Func<Task<TEntity>> valueGetter, 
            MemoryCacheEntryOptions cacheOptions, Dictionary<string, string> tags = null)
        {
            _cacheKeyUsage.AddOrUpdate(key,
                v => new CacheItemUsage { Type = typeof(TEntity), UsageCounter = 0 },
                (k, v) =>
                {
                    v.UsageCounter += 1;
                    return v;
                });

            return _memoryCache.GetOrCreateAsync(key, v =>
            {
                if (tags?.Count > 0)
                {
                    var cancelationSource = new CancellationTokenSource();

                    foreach (var tag in tags)
                    {
                        var sources = GetCancelationTokens(tag.Key, tag.Value);
                        sources.TryAdd(key, cancelationSource);
                    }
                    var changeNotification = new ChangeNotification { Source = cancelationSource, Tags = tags };

                    cacheOptions.AddExpirationToken(new CancellationChangeToken(cancelationSource.Token));
                    cacheOptions.RegisterPostEvictionCallback(PostEvictionCallback, changeNotification);
                }
                else
                {
                    cacheOptions.RegisterPostEvictionCallback(PostEvictionCallback);
                }

                v.SetOptions(cacheOptions);
                return valueGetter();
            });
        }

        public TEntity Get<TEntity>(string key)
        {
            var result =  _memoryCache.Get<TEntity>(key);
            if(result != null)
                _cacheKeyUsage.AddOrUpdate(key,
                v => new CacheItemUsage { Type = typeof(TEntity), UsageCounter = 0 },
                (k, v) =>
                {
                    v.UsageCounter += 1;
                    return v;
                });

            return result;
        }

        public async Task Add<TEntity>(string key, TEntity value, MemoryCacheEntryOptions cacheOptions, Dictionary<string, string> tags = null)
        {
            await GetOrAdd(key, () => Task.FromResult(value), cacheOptions, tags).ConfigureAwait(false);
        }

        public void Remove(string key)
        {
            _memoryCache.Remove(key as string);
        }

        public void Remove(Dictionary<string, string> tags)
        {
            foreach (var tag in tags)
            {
                var sources = GetCancelationTokens(tag.Key, tag.Value);
                foreach (var token in sources)
                {
                    token.Value.Cancel();
                }
            }
        }

        public IReadOnlyDictionary<string, CacheItemUsage> GetUsage()
        {            
            return new ReadOnlyDictionary<string, CacheItemUsage>(_cacheKeyUsage);
        }

        public IReadOnlyDictionary<string, List<string>> GetCancelationTokens()
        {
            return new ReadOnlyDictionary<string, List<string>>(
                _cancelationTokens.ToDictionary(k => k.Key, k => k.Value.Select(v => $"TagValue: {v.Key}, Count: {v.Value.Count}").ToList()));
        }

        private ConcurrentDictionary<string, CancellationTokenSource> GetCancelationTokens(string tagName, string tagValue)
        {
            var sourcesByTag = _cancelationTokens.GetOrAdd(tagName, new ConcurrentDictionary<string, ConcurrentDictionary<string, CancellationTokenSource>>());

            return sourcesByTag.GetOrAdd(tagValue, new ConcurrentDictionary<string, CancellationTokenSource>());
        }

        private void PostEvictionCallback(object keyObject, object value, EvictionReason reason, object state)
        {
            var key = (string)keyObject;

            if (state is ChangeNotification notification)
            {                
                foreach (var item in notification.Tags)
                {
                    var source = GetCancelationTokens(item.Key, item.Value);
                    source.TryRemove(key, out _);
                }
                notification.Source.Dispose();
            }

            _cacheKeyUsage.TryRemove(key, out var usageCounter);
            _logger.Information("Cache item evicted", new Dictionary<string, string> {
                {"Key", key },
                {"Usage", usageCounter.UsageCounter.ToString() },
                { "Entity", usageCounter.Type.Name},
                { "Reason", reason.ToString()}
            });
        }        

        private static void ReportUsage(object state)
        {
            if (state is EnrichedMemoryCache memoryCahceInstance)
            {
                var usage = memoryCahceInstance._cacheKeyUsage
                    .GroupBy(v => v.Value.Type)
                    .ToDictionary( v=>v.Key.Name, v =>$"Items: {v.Count()}, TotalUsage: {v.Sum(v => v.Value.UsageCounter)}");                

                memoryCahceInstance._logger.Information("Cache usage", usage);
            }
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                ReportUsage(this);
                _timer.Dispose();
                _timer = null;
            }
        }        

        private class ChangeNotification
        {
            public Dictionary<string, string> Tags { get; set; }

            public CancellationTokenSource Source { get; set; }
        }

        public class CacheItemUsage
        {
            public long UsageCounter { get; set; }

            public Type Type { get; set; }
        }        
    }
}