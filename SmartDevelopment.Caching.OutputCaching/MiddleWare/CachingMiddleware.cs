using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SmartDevelopment.Caching.EnrichedMemoryCache;
using SmartDevelopment.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartDevelopment.Caching.OutputCaching
{
    public partial class CachingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IEnrichedMemoryCache _memoryCache;
        private readonly ResponseCachingSettings _settings;
        private readonly ILogger<CachingMiddleware> _logger;

        public CachingMiddleware(RequestDelegate next, IEnrichedMemoryCache memoryCache, 
            IOptions<ResponseCachingSettings> settings, ILogger<CachingMiddleware> logger)
        {
            _next = next;
            _memoryCache = memoryCache;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var requestFeatures = context.Features.Get<IHttpRequestFeature>();

            if (!_settings.Enabled || !HttpMethods.IsGet(requestFeatures.Method) || (context.Request.GetTypedHeaders().CacheControl?.NoCache ?? false))
            {
                await _next.Invoke(context);
                return;
            }

            var cacheKey = requestFeatures.RawTarget;

            try
            {
                CachedResponse cachedResponse = null;
                if (context.User.Identity.IsAuthenticated)
                {
                    cachedResponse = _memoryCache.Get<CachedResponse>($"{cacheKey}_u:{context.User.FindFirst(ClaimTypes.NameIdentifier).Value}");
                    if (cachedResponse == null)
                    {
                        cachedResponse = _memoryCache.Get<CachedResponse>(cacheKey);

                        if (cachedResponse?.ForAnonymusUsers ?? false)
                            cachedResponse = null;
                    }
                }
                else
                {
                    cachedResponse = _memoryCache.Get<CachedResponse>(cacheKey);
                }

                if (cachedResponse != null)
                {
                    await cachedResponse.Apply(context);
                    return;
                }
            }
            catch(Exception ex) {
                _logger.Exception(ex);
            }

            var cachedItem = await CaptureResponse(context);
            if (cachedItem != null)
            {
                try
                {
                    if (context.Items.TryGetValue(Consts.IsCachebleKey, out object isCacheble) && (isCacheble is bool) && (bool)isCacheble)
                    {
                        cacheKey = requestFeatures.RawTarget;
                        if (context.Items.TryGetValue(Consts.IsUserSpecificKey, out object isUserSpecific)
                            && (isUserSpecific is bool) && (bool)isUserSpecific)
                        {
                            if (context.User.Identity.IsAuthenticated)
                                cacheKey = $"{cacheKey}_u:{context.User.FindFirst(ClaimTypes.NameIdentifier).Value}";
                            else
                                cachedItem.ForAnonymusUsers = true;
                        }

                        var slidingDuration = (context.Items.TryGetValue(Consts.SlidingDurationKey, out object slidingDurationO) && (slidingDurationO is int)) ?
                            TimeSpan.FromSeconds((int)slidingDurationO) : TimeSpan.Zero;

                        var duration = (context.Items.TryGetValue(Consts.DurationKey, out object durationO) && (durationO is int)) ?
                            TimeSpan.FromSeconds((int)durationO) :
                            TimeSpan.FromSeconds(_settings.MaxCacheInSec);

                        var options = new MemoryCacheEntryOptions();
                        if (slidingDuration > TimeSpan.Zero)
                            options.SlidingExpiration = slidingDuration;
                        else
                            options.AbsoluteExpirationRelativeToNow = duration;

                        Dictionary<string, string> tagsToApply = null;
                        if (context.Items.TryGetValue(Consts.CachedObjectTags, out var tagsO) && tagsO is Dictionary<string, string> tags)
                            tagsToApply = tags;

                        _memoryCache.Add(cacheKey, cachedItem, options, tagsToApply);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Exception(ex);
                }
            }
        }

        private async Task<CachedResponse> CaptureResponse(HttpContext context)
        {
            var responseStream = context.Response.Body;

            using var buffer = new MemoryStream();
            try
            {
                context.Response.Body = buffer;

                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
            }
            finally
            {
                context.Response.Body = responseStream;
            }

            if (buffer.Length == 0) return null;

            var bytes = buffer.ToArray();

            await responseStream.WriteAsync(bytes, 0, bytes.Length);

            if (context.Response.StatusCode != 200) return null;

            return new CachedResponse(bytes, context.Response.Headers);
        }
    }
}
