using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using SmartDevelopment.Messaging;
using System.Collections.Generic;
using System.Linq;

namespace SmartDevelopment.AzureStorage.Queues
{
    public interface IQueue<TMessage> : IChannelSender<TMessage> 
        where TMessage : class
    {
        string QueueName { get; }

        Task<QueueMessage<TMessage>> Get();

        Task Delete(QueueMessage<TMessage> message);
    }

    public abstract class BaseQueue<TMessage> : IChannelSender<TMessage>
        where TMessage : class
    {
        protected readonly CloudQueue Queue;

        protected BaseQueue(ConnectionSettings connectionSettings, string queueName)
        {
            QueueName = queueName;
            var account = CloudStorageAccount.Parse(connectionSettings.ConnectionString);
            var client = account.CreateCloudQueueClient();
            Queue = client.GetQueueReference(QueueName.ToLower());
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
            var item = new CloudQueueMessage(payload);
            return Queue.AddMessageAsync(item, null, initialDelay, null, null);
        }

        public Task Add(List<TMessage> message, TimeSpan? initialDelay = null)
        {
            return Task.WhenAll(message.Select(v => Add(v, initialDelay)));
        }

        public async Task<QueueMessage<TMessage>> Get()
        {
            var item = await Queue.GetMessageAsync().ConfigureAwait(false);
            if (item == null)
                return null;
            var  message = JsonConvert.DeserializeObject<TMessage>(item.AsString);
            return new QueueMessage<TMessage>(message, item);
        }

        public Task Delete(QueueMessage<TMessage> message)
        {
            return Queue.DeleteMessageAsync(message.OriginalMessage);
        }        
    }

    public class QueueMessage<TMessage> where TMessage : class
    {
        public QueueMessage(TMessage message, CloudQueueMessage originalMessage)
        {
            Message = message;
            OriginalMessage = originalMessage;
        }

        public TMessage Message { get; }

        public CloudQueueMessage OriginalMessage { get; }
    }
}
