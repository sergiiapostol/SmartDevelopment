using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using Newtonsoft.Json;
using SmartDevelopment.Logging;
using SmartDevelopment.Messaging;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SmartDevelopment.ServiceBus
{
    class ChannelReceiver<TMessage>
        where TMessage : class
    {
        public ChannelReceiver(IReceiverClient receiverClient,
            Func<TMessage, Task> messageHandler,
            ILogger logger)
        {
            receiverClient.RegisterMessageHandler(async (v, token) =>
            {
                try
                {
                    var messageObject = JsonConvert.DeserializeObject<TMessage>(Encoding.UTF8.GetString(v.Body));
                    await messageHandler(messageObject).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                }
            }, new MessageHandlerOptions(v =>
            {
                logger.Exception(v.Exception);
                return Task.CompletedTask;
            })
            {
                AutoComplete = true,
                MaxConcurrentCalls = 1
            });
        }
    }

    public abstract class BaseTopicReceiver<TMessage> : IChannelReceiver<TMessage>, IAsyncDisposable
        where TMessage : class
    {
        private readonly ISubscriptionClient _client;
        private readonly ChannelReceiver<TMessage> _channel;
        private readonly ConnectionSettings _connectionSettings;
        private readonly string _subscriptionName;

        protected BaseTopicReceiver(ConnectionSettings connectionSettings, string topicName, string subscriptionName, ILogger logger)
        {
            _connectionSettings = connectionSettings;
            _subscriptionName = subscriptionName;
            ChannelName = topicName;
            _client = new SubscriptionClient(connectionSettings.ConnectionString, topicName, subscriptionName);
            _channel = new ChannelReceiver<TMessage>(_client, v => ProcessMessage(v), logger);
        }

        public string ChannelName { get; }

        public async ValueTask DisposeAsync()
        {
            await _client.CloseAsync().ConfigureAwait(false);
        }

        public async Task Init()
        {
            var managementClient = new ManagementClient(_connectionSettings.ConnectionString);
            if (!await managementClient.SubscriptionExistsAsync(ChannelName, _subscriptionName).ConfigureAwait(false))
            {
                await managementClient.CreateSubscriptionAsync(new SubscriptionDescription(ChannelName, _subscriptionName)).ConfigureAwait(false);
            }
            await managementClient.CloseAsync().ConfigureAwait(false);
        }

        public abstract Task ProcessMessage(TMessage message);
    }

    public abstract class BaseQueueReceiver<TMessage> : IChannelReceiver<TMessage>, IAsyncDisposable
        where TMessage : class
    {
        private readonly IQueueClient _client;
        private readonly ChannelReceiver<TMessage> _channel;
        protected BaseQueueReceiver(ConnectionSettings connectionSettings, string queueName, ILogger logger)
        {
            ChannelName = queueName;
            _client = new QueueClient(connectionSettings.ConnectionString, queueName);
            _channel = new ChannelReceiver<TMessage>(_client, v => ProcessMessage(v), logger);
        }

        public string ChannelName { get; }

        public async ValueTask DisposeAsync()
        {
            await _client.CloseAsync().ConfigureAwait(false);
        }

        public Task Init()
        {
            return Task.CompletedTask;
        }

        public abstract Task ProcessMessage(TMessage message);
    }
}
