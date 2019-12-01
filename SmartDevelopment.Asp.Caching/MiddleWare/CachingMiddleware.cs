using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartDevelopment.Asp.Caching
{
    public partial class CachingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _memoryCache;
        private readonly ResponseCachingSettings _settings;
        private readonly CacheBustingService _cacheBusting;

        public CachingMiddleware(RequestDelegate next, IMemoryCache memoryCache, IOptions<ResponseCachingSettings> settings, CacheBustingService cacheBusting)
        {
            _next = next;
            _memoryCache = memoryCache;
            _settings = settings.Value;
            _cacheBusting = cacheBusting;
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
                    object cachedResponseO = _memoryCache.Get($"{cacheKey}_u:{context.User.FindFirst(ClaimTypes.NameIdentifier).Value}");
                    if (cachedResponseO == null)
                    {
                        cachedResponseO = _memoryCache.Get(cacheKey);

                        if ((cachedResponseO as CachedResponse)?.ForAnonymusUsers ?? false)
                            cachedResponseO = null;
                    }

                    cachedResponse = cachedResponseO as CachedResponse;
                }
                else
                {
                    cachedResponse = _memoryCache.Get(cacheKey) as CachedResponse;
                }

                if (cachedResponse != null)
                {
                    await cachedResponse.Apply(context).ConfigureAwait(false);
                    return;
                }
            }
            catch (Exception ex)
            {
                int i = 0;
            }

            var cachedItem = await CaptureResponse(context).ConfigureAwait(false);
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

                        var duration = (context.Items.TryGetValue(Consts.DurationKey, out object durationO) && (durationO is int)) ?
                            TimeSpan.FromSeconds((int)durationO) :
                            TimeSpan.FromSeconds(_settings.MaxCacheInSec);

                        var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration };

                        _cacheBusting.AsociateToken(context, cacheKey, options);

                        _memoryCache.Set(cacheKey, cachedItem, options);
                    }
                }
                catch (Exception ex)
                {
                    int i = 0;
                }
            }
        }

        private async Task<CachedResponse> CaptureResponse(HttpContext context)
        {
            var responseStream = context.Response.Body;

            using (var buffer = new MemoryStream())
            {
                try
                {
                    context.Response.Body = buffer;

                    await _next.Invoke(context);
                }
                finally
                {
                    context.Response.Body = responseStream;
                }

                if (buffer.Length == 0) return null;

                var bytes = buffer.ToArray();

                await responseStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);

                if (context.Response.StatusCode != 200) return null;

                return new CachedResponse(bytes, context.Response.Headers);
            }
        }
    }
}
