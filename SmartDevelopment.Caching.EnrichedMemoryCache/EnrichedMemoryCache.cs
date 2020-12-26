using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
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
        private readonly EnrichedMemoryCacheSettings _settings;


        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, CancellationTokenSource>>> _cancelationTokens =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, CancellationTokenSource>>>();

        private readonly ConcurrentDictionary<string, CacheItemUsage> _cacheKeyUsage = new ConcurrentDictionary<string, CacheItemUsage>();

        private Timer _timer;

        protected EnrichedMemoryCache(ILogger logger, IMemoryCache memoryCache, IOptions<EnrichedMemoryCacheSettings> settings)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _timer = new Timer(ReportUsage, this, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
            _settings = settings.Value ?? new EnrichedMemoryCacheSettings { IsEnabled = true };
        }

        public EnrichedMemoryCache(IMemoryCache memoryCache, ILogger<EnrichedMemoryCache> logger, IOptions<EnrichedMemoryCacheSettings> settings)
            : this(logger, memoryCache, settings)
        {
        }

        public Task<TEntity> GetOrAdd<TEntity>(string key, Func<Task<TEntity>> valueGetter,
            MemoryCacheEntryOptions cacheOptions, Dictionary<string, string> tags = null)
        {
            if (!_settings.IsEnabled || string.IsNullOrWhiteSpace(key))
                return valueGetter();

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
            if (!_settings.IsEnabled || string.IsNullOrWhiteSpace(key))
                return default;

            var result = _memoryCache.Get<TEntity>(key);
            if (result != null && !result.Equals(default(TEntity)))
            {
                _cacheKeyUsage.AddOrUpdate(key,
                v => new CacheItemUsage { Type = typeof(TEntity), UsageCounter = 0 },
                (k, v) =>
                {
                    v.UsageCounter += 1;
                    return v;
                });
            }

            return result;
        }

        public void Add<TEntity>(string key, TEntity value, MemoryCacheEntryOptions cacheOptions, Dictionary<string, string> tags = null)
        {
            if (!_settings.IsEnabled || string.IsNullOrWhiteSpace(key))
                return;

            _cacheKeyUsage.AddOrUpdate(key,
                v => new CacheItemUsage { Type = typeof(TEntity), UsageCounter = 0 },
                (k, v) =>
                {
                    v.UsageCounter += 1;
                    return v;
                });

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

            _memoryCache.Set(key, value, cacheOptions);
        }

        public virtual Task Remove(string key)
        {
            if (_settings.IsEnabled)
                _memoryCache.Remove(key as string);

            return Task.CompletedTask;
        }

        public virtual Task Remove(Dictionary<string, string> tags)
        {
            if (_settings.IsEnabled)
                foreach (var tag in tags)
                {
                    var sources = GetCancelationTokens(tag.Key, tag.Value);
                    foreach (var token in sources)
                    {
                        token.Value.Cancel();
                    }
                }

            return Task.CompletedTask;
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
            _logger.Debug("Cache item evicted", new Dictionary<string, string> {
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
                try
                {
                    var usage = new Dictionary<string, CacheItemUsage>(memoryCahceInstance._cacheKeyUsage)
                        .GroupBy(v => v.Value.Type.FullName)
                        .ToDictionary(v => v.Key, v => $"Items: {v.Count()}, TotalUsage: {v.Sum(v => v.Value.UsageCounter)}");

                    memoryCahceInstance._logger.Information("Cache usage", usage);
                }
                catch (Exception ex)
                {
                    memoryCahceInstance._logger.Debug(ex);
                }
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