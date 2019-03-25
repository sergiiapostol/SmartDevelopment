using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartDevelopment.AzureStorage.Queues
{
    public class QueuesInitializator
    {
        private IEnumerable<IQueue> _queues { get; }

        private readonly ILogger<QueuesInitializator> _logger;

        public QueuesInitializator(IEnumerable<IQueue> queues, ILogger<QueuesInitializator> logger)
        {
            _queues = queues;
            _logger = logger;
        }

        public async Task Init()
        {
            foreach (var queue in _queues)
            {
                try
                {
                    await queue.Init().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"failed to init queue {queue.GetType().FullName}");
                }
            }
        }
    }
}
