using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartDevelopment.Messaging
{
    public class ChannelsInitializator
    {
        private readonly IEnumerable<IChannel> _channels;

        private readonly ILogger<ChannelsInitializator> _logger;

        public ChannelsInitializator(IEnumerable<IChannel> queues, ILogger<ChannelsInitializator> logger)
        {
            _channels = queues;
            _logger = logger;
        }

        public async Task Init()
        {
            foreach (var queue in _channels)
            {
                try
                {
                    await queue.Init();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"failed to init queue {queue.GetType().FullName}");
                }
            }
        }
    }
}
