using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDevelopment.Logging;
using SmartDevelopment.ServiceBus;

namespace SmartDevelopment.SampleApp.AspCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class ServiceBusController : ControllerBase
    {
        private readonly TestQueueSender _testQueueSender;
        private readonly TestQeueuReceiver _testQeueuReceiver;
        private readonly TestTopicSender _testTopicSender;
        private readonly TestTopicReceiver _testTopicReceiver;
        private readonly Random _random;

        public ServiceBusController(TestQueueSender testQueueSender, TestQeueuReceiver testQeueuReceiver,
            TestTopicSender testTopicSender, TestTopicReceiver testTopicReceiver)
        {
            _testQueueSender = testQueueSender;
            _testQeueuReceiver = testQeueuReceiver;
            _testTopicSender = testTopicSender;
            _testTopicReceiver = testTopicReceiver;
            _random = new Random();
        }

        [HttpPost, Route("TestQueue")]
        public async Task<ActionResult> TestQueue()
        {
            await _testQueueSender.Add(new TestMessage { A = _random.Next() });
            return Ok();
        }

        [HttpPost, Route("TestTopic")]
        public async Task<ActionResult> TestTopic()
        {
            await _testTopicSender.Add(new TestMessage { A = _random.Next() });
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