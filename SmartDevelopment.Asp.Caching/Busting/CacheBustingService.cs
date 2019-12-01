using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace SmartDevelopment.Asp.Caching
{
    public interface ICacheBustingService
    {
        void ReleaseCache(Dictionary<string, string> tags);
        void TagCache(HttpContext context, Dictionary<string, string> tags);
    }

    public class CacheBustingService : ICacheBustingService
    {
        private readonly ResponseCachingSettings _settings;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, CancellationTokenSource>>> _cancelationTokens =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, CancellationTokenSource>>>();
        private const string ContextItemsKey = "cachetag";

        public CacheBustingService(IOptions<ResponseCachingSettings> settings)
        {
            _settings = settings.Value;
        }

        public void ReleaseCache(Dictionary<string, string> tags)
        {
            try
            {
                if (!_settings.Enabled)
                    return;

                foreach (var tag in tags)
                {
                    var sources = GetTokens(tag.Key, tag.Value);
                    foreach (var token in sources)
                    {
                        token.Value.Cancel();
                    }
                }
            }
            catch (Exception ex)
            {
                int i = 0;
            }
        }

        public void TagCache(HttpContext context, Dictionary<string, string> tags)
        {
            try
            {
                if (!_settings.Enabled)
                    return;

                context.Items[ContextItemsKey] = tags;
            }catch (Exception ex)
            {
                int i = 0;
            }
        }

        internal void AsociateToken(HttpContext context, string key, MemoryCacheEntryOptions cacheOptions)
        {
            if (context.Items.TryGetValue(ContextItemsKey, out object tagsO))
            {
                var tags = tagsO as Dictionary<string, string>;

                var cancelationSource = new CancellationTokenSource();

                foreach (var tag in tags)
                {
                    var sources = GetTokens(tag.Key, tag.Value);
                    if(!sources.TryAdd(key, cancelationSource))
                    {
                        int i = 0;
                        ////something is wrong here
                    }
                }
                var changeNotification = new ChangeNotification { Source = cancelationSource, Tags = tags };

                cacheOptions.AddExpirationToken(new CancellationChangeToken(cancelationSource.Token));
                cacheOptions.RegisterPostEvictionCallback(PostEvictionCallback, changeNotification);
            }
        }

        private void PostEvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            var notification = state as ChangeNotification;
            foreach (var item in notification.Tags)
            {
                var source = GetTokens(item.Key, item.Value);
                if (!source.TryRemove(key.ToString(), out CancellationTokenSource tokenSource))
                {
                    int i = 0;
                    ////something is wrong here
                }
            }
            notification.Source.Dispose();
        }

        private class ChangeNotification
        {
            public Dictionary<string, string> Tags { get; set; }

            public CancellationTokenSource Source { get; set; }
        }

        private ConcurrentDictionary<string, CancellationTokenSource> GetTokens(string tagName, string tagValue)
        {
            var sourcesByTag = _cancelationTokens.GetOrAdd(tagName, new ConcurrentDictionary<string, ConcurrentDictionary<string, CancellationTokenSource>>());

            return sourcesByTag.GetOrAdd(tagValue, new ConcurrentDictionary<string, CancellationTokenSource>());
        }
    }
}