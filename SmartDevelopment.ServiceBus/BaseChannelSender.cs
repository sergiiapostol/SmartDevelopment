using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using Newtonsoft.Json;
using SmartDevelopment.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartDevelopment.ServiceBus
{
    class ChannelSender<TMessage>
        where TMessage : class
    {
        private readonly ISenderClient _client;

        public ChannelSender(ISenderClient client)
        {
            _client = client;
        }

        private Message CreateMessage(TMessage message, TimeSpan? initialDelay = null)
        {
            var messageObject = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));
            if (initialDelay.HasValue)
                messageObject.ScheduledEnqueueTimeUtc = DateTime.UtcNow.Add(initialDelay.Value);

            return messageObject;
        }

        public Task Add(TMessage message, TimeSpan? initialDelay = null)
        {
            var messageToSend = CreateMessage(message, initialDelay);

            return _client.SendAsync(messageToSend);
        }

        public Task Add(List<TMessage> messages, TimeSpan? initialDelay = null)
        {
            var messagesToSend = messages.Select(v => CreateMessage(v, initialDelay)).ToList();

            return _client.SendAsync(messagesToSend);
        }
    }

    public class BaseQueueSender<TMessage> : IChannelSender<TMessage>, IAsyncDisposable 
        where TMessage : class
    {
        private readonly IQueueClient _client;
        protected readonly ConnectionSettings _connectionSettings;
        private readonly ChannelSender<TMessage> _channel;

        protected BaseQueueSender(ConnectionSettings connectionSettings, string queueName)
        {
            ChannelName = queueName;
            _connectionSettings = connectionSettings;
            _client = new QueueClient(connectionSettings.ConnectionString, ChannelName);
            _channel = new ChannelSender<TMessage>(_client);
        }

        public string ChannelName { get; }

        public Task Add(TMessage message, TimeSpan? initialDelay = null)
        {
            return _channel.Add(message, initialDelay);
        }

        public Task Add(List<TMessage> messages, TimeSpan? initialDelay = null)
        {
            return _channel.Add(messages, initialDelay);
        }

        public async Task Init()
        {
            var managementClient = new ManagementClient(_connectionSettings.ConnectionString);
            if(!await managementClient.QueueExistsAsync(ChannelName).ConfigureAwait(false))
            {
                await managementClient.CreateQueueAsync(new QueueDescription(ChannelName)
                {
                    DefaultMessageTimeToLive = TimeSpan.FromDays(7),
                    EnableBatchedOperations = true,
                    EnableDeadLetteringOnMessageExpiration = true,
                    LockDuration = TimeSpan.FromMinutes(5),
                    MaxDeliveryCount = 10
                }).ConfigureAwait(false);
            }
            await managementClient.CloseAsync().ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await _client.CloseAsync().ConfigureAwait(false);
        }
    }

    public class BaseTopicSender<TMessage> : IChannelSender<TMessage>, IAsyncDisposable
        where TMessage : class
    {
        private readonly ITopicClient _client;
        protected readonly ConnectionSettings _connectionSettings;
        private readonly ChannelSender<TMessage> _channel;

        protected BaseTopicSender(ConnectionSettings connectionSettings, string topicName)
        {
            ChannelName = topicName;
            _connectionSettings = connectionSettings;
            _client = new TopicClient(connectionSettings.ConnectionString, ChannelName);
            _channel = new ChannelSender<TMessage>(_client);
        }

        public string ChannelName { get; }

        public Task Add(TMessage message, TimeSpan? initialDelay = null)
        {
            return _channel.Add(message, initialDelay);
        }

        public Task Add(List<TMessage> messages, TimeSpan? initialDelay = null)
        {
            return _channel.Add(messages, initialDelay);
        }

        public async Task Init()
        {
            var managementClient = new ManagementClient(_connectionSettings.ConnectionString);
            if (!await managementClient.TopicExistsAsync(ChannelName).ConfigureAwait(false))
            {
                await managementClient.CreateTopicAsync(new TopicDescription(ChannelName)
                {
                    DefaultMessageTimeToLive = TimeSpan.FromDays(7),
                    EnableBatchedOperations = true,
                }).ConfigureAwait(false);
            }
            await managementClient.CloseAsync().ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await _client.CloseAsync().ConfigureAwait(false);
        }
    }
}
