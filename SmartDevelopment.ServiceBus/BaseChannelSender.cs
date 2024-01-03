using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using SmartDevelopment.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartDevelopment.ServiceBus
{
    public abstract class ChannelSender<TMessage> : IAsyncDisposable
        where TMessage : class
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _sender;

        public string ChannelName { get; protected set; }

        protected ChannelSender(ConnectionSettings connectionSettings, string targetName)
        {
            ChannelName = targetName;
            _client = new ServiceBusClient(connectionSettings.ConnectionString);
            _sender = _client.CreateSender(targetName);
        }

        private ServiceBusMessage CreateMessage(TMessage message, TimeSpan? initialDelay = null)
        {
            var messageObject = new ServiceBusMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));
            if (initialDelay.HasValue)
                messageObject.ScheduledEnqueueTime = DateTime.UtcNow.Add(initialDelay.Value);

            return messageObject;
        }

        public Task Add(TMessage message, TimeSpan? initialDelay = null)
        {
            var messageToSend = CreateMessage(message, initialDelay);

            return _sender.SendMessageAsync(messageToSend);
        }

        public Task Add(List<TMessage> messages, TimeSpan? initialDelay = null)
        {
            var messagesToSend = messages.Select(v => CreateMessage(v, initialDelay)).ToList();

            return _sender.SendMessagesAsync(messagesToSend);
        }

        public Task Init()
        {
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            await _sender.DisposeAsync();
            await _client.DisposeAsync();
        }
    }

    public abstract class BaseQueueSender<TMessage> : ChannelSender<TMessage>, IChannelSender<TMessage>
        where TMessage : class
    {
        public BaseQueueSender(ConnectionSettings connectionSettings, string queueName) : base(connectionSettings, queueName)
        {
        }
    }

    public abstract class BaseTopicSender<TMessage> : ChannelSender<TMessage>, IChannelSender<TMessage>
        where TMessage : class
    {
        public BaseTopicSender(ConnectionSettings connectionSettings, string topicName) : base(connectionSettings, topicName)
        {
        }
    }
}
