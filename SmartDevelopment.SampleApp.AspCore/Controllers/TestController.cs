using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDevelopment.Caching.OutputCaching;
using SmartDevelopment.Caching.EnrichedMemoryCache;
using SmartDevelopment.Dal.Abstractions;
using SmartDevelopment.Dal.Abstractions.Models;
using SmartDevelopment.Logging;

namespace SmartDevelopment.SampleApp.AspCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private readonly IDal<Identity.Entities.IdentityUser> _dal;
        private readonly OutputCacheManager _outputCacheManager;
        private readonly IEnrichedMemoryCache _enrichedMemoryCache;

        public TestController(ILogger<TestController> logger, IDal<Identity.Entities.IdentityUser> dal, 
            OutputCacheManager outputCacheTagger, IEnrichedMemoryCache enrichedMemoryCache)
        {
            _logger = logger;
            _dal = dal;
            _outputCacheManager = outputCacheTagger;
            _enrichedMemoryCache = enrichedMemoryCache;
        }

        [HttpGet, Route("Logger")]
        public async Task<ActionResult> Logger()
        {
            _logger.Debug("Debug");
            _logger.Debug(new Exception("Debug"));
            _logger.Exception(new Exception("exception"));
            _logger.Information("Information");
            _logger.Trace("Trace");
            _logger.Warning(new Exception("Warning"));
            _logger.Warning("Warning");

            await _dal.SetAsync<Identity.Entities.IdentityUser>(v => v.CreatedAt > DateTime.UtcNow.Date, new List<PropertyUpdate<Identity.Entities.IdentityUser>> {
                new PropertyUpdate<Identity.Entities.IdentityUser>(v=>v.Email, "test@bla.com"),
                new PropertyUpdate<Identity.Entities.IdentityUser>(v=>v.SecurityStamp, "test@bla.com")
            }).ConfigureAwait(false);

            return Ok();
        }


        [OutputCache(false, 5)]
        [HttpGet, Route("Cache")]
        public ActionResult CacheCreate()
        {
            _outputCacheManager.TagCache(ControllerContext.HttpContext, new Dictionary<string, string> { { "TagKey", "TagValue" } });
            return Ok(5);
        }

        [HttpDelete, Route("Cache")]
        public ActionResult CacheDelete()
        {
            _outputCacheManager.ReleaseCache(new Dictionary<string, string> { { "TagKey", "TagValue" } });
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