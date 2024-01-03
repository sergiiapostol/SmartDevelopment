using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using SmartDevelopment.Messaging;
using System.Collections.Generic;
using System.Linq;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace SmartDevelopment.AzureStorage.Queues
{
    public interface IQueue<TMessage> : IChannelSender<TMessage> 
        where TMessage : class
    {
        string QueueName { get; }

        Task<QueueMessage<TMessage>> Get();

        Task Delete(QueueMessage<TMessage> message);
    }

    public abstract class BaseQueue<TMessage> : IQueue<TMessage>
        where TMessage : class
    {
        protected readonly QueueClient Queue;

        protected BaseQueue(ConnectionSettings connectionSettings, string queueName)
        {
            QueueName = queueName;
            Queue = new QueueClient(connectionSettings.ConnectionString, QueueName.ToLower());
        }

        public string QueueName { get; }

        public string ChannelName => QueueName;

        public virtual Task Init()
        {
            return Queue.CreateIfNotExistsAsync();
        }

        public Task Add(TMessage message, TimeSpan? initialDelay = null)
        {
            var payload = JsonConvert.SerializeObject(message);
            return Queue.SendMessageAsync(payload, initialDelay);
        }

        public Task Add(List<TMessage> message, TimeSpan? initialDelay = null)
        {
            return Task.WhenAll(message.Select(v => Add(v, initialDelay)));
        }

        public async Task<QueueMessage<TMessage>> Get()
        {
            var item = await Queue.ReceiveMessageAsync();
            if (item == null)
                return null;
            var  message = item.Value.Body.ToObjectFromJson<TMessage>();
            return new QueueMessage<TMessage>(message, item.Value);
        }

        public Task Delete(QueueMessage<TMessage> message)
        {
            return Queue.DeleteMessageAsync(message.RawMessage.MessageId, message.RawMessage.PopReceipt);
        }        
    }

    public class QueueMessage<TMessage> where TMessage : class
    {
        public QueueMessage(TMessage message, QueueMessage rawMessage)
        {
            Message = message;
            RawMessage = rawMessage;
        }

        public TMessage Message { get; }

        public QueueMessage RawMessage { get; }
    }
}
