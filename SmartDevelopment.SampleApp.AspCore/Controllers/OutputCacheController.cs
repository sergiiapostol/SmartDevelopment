using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDevelopment.Caching.OutputCaching;

namespace SmartDevelopment.SampleApp.AspCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class OutputCacheController : ControllerBase
    {
        private readonly OutputCacheManager _outputCacheManager;

        public OutputCacheController(OutputCacheManager outputCacheTagger)
        {
            _outputCacheManager = outputCacheTagger;
        }


        [OutputCache(false, SlidingDurationInSec = 500)]
        [HttpGet, Route("Cache")]
        public object CacheCreate()
        {
            OutputCacheManager.TagCache(ControllerContext.HttpContext, new Dictionary<string, string> { { "TagKey", "TagValue" } });

            return null;
        }
    }
}