using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDevelopment.Caching.OutputCaching;
using SmartDevelopment.Caching.EnrichedMemoryCache;

namespace SmartDevelopment.SampleApp.AspCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class CacheController : ControllerBase
    {
        private readonly OutputCacheManager _outputCacheManager;
        private readonly IEnrichedMemoryCache _enrichedMemoryCache;

        public CacheController(OutputCacheManager outputCacheTagger, IEnrichedMemoryCache enrichedMemoryCache)
        {
            _outputCacheManager = outputCacheTagger;
            _enrichedMemoryCache = enrichedMemoryCache;
        }


        [OutputCache(false, SlidingDurationInSec = 500)]
        [HttpGet, Route("Cache")]
        public async Task<ActionResult> CacheCreate()
        {            
            _outputCacheManager.TagCache(ControllerContext.HttpContext, new Dictionary<string, string> { { "TagKey", "TagValue" } });
            
            await _enrichedMemoryCache.GetOrAdd("TestValue1", () => Task.FromResult(1), new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions { },
                new Dictionary<string, string> { { "TagKey1", "TagValue" } });
            
            await _enrichedMemoryCache.GetOrAdd("TestValue2", () => Task.FromResult(2), new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions { },
                new Dictionary<string, string> { { "TagKey1", "TagValue" } });

            var a = _enrichedMemoryCache.Get<object>("TestValue2");

            return Ok(5);
        }

        [HttpDelete, Route("Cache")]
        public async Task<ActionResult> CacheDelete()
        {
            await _enrichedMemoryCache.Remove(new Dictionary<string, string> { { "TagKey1", "TagValue" } });
            return Ok();
        }

        [HttpGet, Route("CacheStatus")]
        public ActionResult CacheStatus()
        {
            var cacheStatus = _enrichedMemoryCache.GetUsage();
            var tokens = _enrichedMemoryCache.GetCancelationTokens();
            return Ok(new { Usage = cacheStatus.ToDictionary(v=>v.Key, v=>$"Type: {v.Value.Type.Name}, Cound: {v.Value.UsageCounter}"), Tokens = tokens});
        }
    }
}