using System;
using System.Threading.Tasks;
using SmartDevelopment.Messaging;
using System.Collections.Generic;
using System.Linq;
using Azure.Storage.Queues;

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
            Queue = new QueueClient(connectionSettings.ConnectionString, queueName.ToLower());
        }

        public string QueueName { get; }

        public string ChannelName => QueueName;

        public virtual Task Init()
        {
            return Queue.CreateIfNotExistsAsync();
        }

        public Task Add(TMessage message, TimeSpan? initialDelay = null)
        {
            return Queue.SendMessageAsync(BinaryData.FromObjectAsJson(message), initialDelay);
        }

        public Task Add(List<TMessage> message, TimeSpan? initialDelay = null)
        {
            return Task.WhenAll(message.Select(v => Add(v, initialDelay)));
        }

        public async Task<QueueMessage<TMessage>> Get()
        {
            var item = await Queue.ReceiveMessageAsync();
            if (item.Value == null)
                return null;
            return new QueueMessage<TMessage>(item.Value.Body.ToObjectFromJson<TMessage>(), item.Value);
        }

        public Task Delete(QueueMessage<TMessage> message)
        {
            return Queue.DeleteMessageAsync(message.OriginalMessage.MessageId, message.OriginalMessage.PopReceipt);
        }        
    }

    public class QueueMessage<TMessage> where TMessage : class
    {
        public QueueMessage(TMessage message, Azure.Storage.Queues.Models.QueueMessage originalMessage)
        {
            Message = message;
            OriginalMessage = originalMessage;
        }

        public TMessage Message { get; }

        internal Azure.Storage.Queues.Models.QueueMessage OriginalMessage { get; }
    }
}
