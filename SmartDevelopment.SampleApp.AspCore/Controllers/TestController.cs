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
using SmartDevelopment.ServiceBus;

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
        private readonly TestQueueSender _testQueueSender;
        private readonly TestQeueuReceiver _testQeueuReceiver;
        private readonly TestTopicSender _testTopicSender;
        private readonly TestTopicReceiver _testTopicReceiver;
        private readonly Random _random;

        public TestController(ILogger<TestController> logger, IDal<Identity.Entities.IdentityUser> dal, 
            OutputCacheManager outputCacheTagger, IEnrichedMemoryCache enrichedMemoryCache,
            TestQueueSender testQueueSender, TestQeueuReceiver testQeueuReceiver,
            TestTopicSender testTopicSender, TestTopicReceiver testTopicReceiver)
        {
            _logger = logger;
            _dal = dal;
            _outputCacheManager = outputCacheTagger;
            _enrichedMemoryCache = enrichedMemoryCache;
            _testQueueSender = testQueueSender;
            _testQeueuReceiver = testQeueuReceiver;
            _testTopicSender = testTopicSender;
            _testTopicReceiver = testTopicReceiver;
            _random = new Random();
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

            await _dal.SetAsync(v => v.CreatedAt > DateTime.UtcNow.Date, new List<PropertyUpdate<Identity.Entities.IdentityUser>> {
                new PropertyUpdate<Identity.Entities.IdentityUser>(v=>v.Email, "test@bla.com"),
                new PropertyUpdate<Identity.Entities.IdentityUser>(v=>v.SecurityStamp, "test@bla.com")
            }).ConfigureAwait(false);

            return Ok();
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

        [HttpPost, Route("TestQueue")]
        public async Task<ActionResult> TestQueue()
        {
            await _testQueueSender.Add(new TestMessage { A = _random.Next() }).ConfigureAwait(false);
            return Ok();
        }

        [HttpPost, Route("TestTopic")]
        public async Task<ActionResult> TestTopic()
        {
            await _testTopicSender.Add(new TestMessage { A = _random.Next() }).ConfigureAwait(false);
            return Ok();
        }
    }

    public class TestMessage
    {
        public int A { get; set; }
    }
    public class TestQueueSender : BaseQueueSender<TestMessage>
    {
        public TestQueueSender(ConnectionSettings connectionSettings) : base(connectionSettings, "TestQueue")
        {
        }
    }

    public class TestQeueuReceiver : BaseQueueReceiver<TestMessage>
    {
        private readonly ILogger _logger;

        public TestQeueuReceiver(ConnectionSettings connectionSettings, ILogger<TestQeueuReceiver> logger) : 
            base(connectionSettings, "TestQueue", logger)
        {
            _logger = logger;
        }

        public override Task ProcessMessage(TestMessage message)
        {
            _logger.Information("Qeueu: " + message.A.ToString());
            return Task.CompletedTask;
        }
    }

    public class TestTopicSender : BaseTopicSender<TestMessage>
    {
        public TestTopicSender(ConnectionSettings connectionSettings) : base(connectionSettings, "TestTopic")
        {
        }
    }

    public class TestTopicReceiver : BaseTopicReceiver<TestMessage>
    {
        private readonly ILogger _logger;

        public TestTopicReceiver(ConnectionSettings connectionSettings, ILogger<TestQeueuReceiver> logger) :
            base(connectionSettings, "TestTopic", "testsubscriber", logger)
        {
            _logger = logger;
        }

        public override Task ProcessMessage(TestMessage message)
        {
            _logger.Information("Topic:" + message.A.ToString());
            return Task.CompletedTask;
        }
    }
}