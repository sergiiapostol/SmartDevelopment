﻿using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace SmartDevelopment.AzureStorage.Queues
{
    public interface IQueue
    {
        Task Init();
    }

    public interface IQueue<TMessage> : IQueue where TMessage : class
    {
        Task Add(TMessage message, TimeSpan? initialDelay = null);

        Task<QueueMessage<TMessage>> Get();

        Task Delete(QueueMessage<TMessage> message);
    }

    public abstract class BaseQueue<TMessage> : IQueue<TMessage>
        where TMessage : class
    {
        protected readonly CloudQueue Queue;

        protected BaseQueue(IOptions<ConnectionSettings> connectionSettings)
        {
            var account = CloudStorageAccount.Parse(connectionSettings.Value.ConnectionString);
            var client = account.CreateCloudQueueClient();
            Queue = client.GetQueueReference(_QueueName.ToLower());
        }

        protected abstract string _QueueName { get; }

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

        public async Task<QueueMessage<TMessage>> Get()
        {
            var item = await Queue.GetMessageAsync().ConfigureAwait(false);
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