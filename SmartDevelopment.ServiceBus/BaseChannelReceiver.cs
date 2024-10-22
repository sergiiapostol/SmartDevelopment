using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using SmartDevelopment.Messaging;
using System;
using System.Text;
using System.Threading.Tasks;
using ILogger = SmartDevelopment.Logging.ILogger;

namespace SmartDevelopment.ServiceBus
{
    public abstract class ChannelReceiver<TMessage> : IAsyncDisposable
        where TMessage : class
    {
        private readonly ILogger _logger;
        protected ServiceBusProcessor Processor;
        protected ServiceBusClient Client;

        protected ChannelReceiver(ILogger logger)
        {                       
            _logger = logger;            
        }

        public string ChannelName { get; protected set; }

        public async ValueTask DisposeAsync()
        {
            await Processor.StopProcessingAsync();
            Processor.ProcessMessageAsync -= MessageHandler;
            Processor.ProcessErrorAsync -= ErrorHandler;            
            await Processor.DisposeAsync();
            await Client.DisposeAsync();

            GC.SuppressFinalize(this);
        }

        public Task Init()
        {
            Processor.ProcessMessageAsync += MessageHandler;
            Processor.ProcessErrorAsync += ErrorHandler;
            return Processor.StartProcessingAsync();
        }

        public abstract Task ProcessMessage(TMessage message);

        private async Task MessageHandler(ProcessMessageEventArgs args)
        {
            try
            {
                var messageObject = JsonConvert.DeserializeObject<TMessage>(Encoding.UTF8.GetString(args.Message.Body));
                await ProcessMessage(messageObject);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
            }

            await args.CompleteMessageAsync(args.Message);
        }

        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            _logger.Exception(args.Exception);
            return Task.CompletedTask;
        }
    }

    public abstract class BaseTopicReceiver<TMessage> : ChannelReceiver<TMessage>, IChannelReceiver<TMessage>
        where TMessage : class
    {
        protected BaseTopicReceiver(ConnectionSettings connectionSettings, string topicName, string subscriptionName, ILogger logger)
            :base(logger)
        {
            ChannelName = topicName;
            Client = new ServiceBusClient(connectionSettings.ConnectionString);
            Processor = Client.CreateProcessor(topicName, subscriptionName, 
                new ServiceBusProcessorOptions { AutoCompleteMessages = true, MaxConcurrentCalls = 1 });
        }
    }

    public abstract class BaseQueueReceiver<TMessage> : ChannelReceiver<TMessage>, IChannelReceiver<TMessage>
        where TMessage : class
    {
        protected BaseQueueReceiver(ConnectionSettings connectionSettings, string queueName, ILogger logger)
            :base(logger) 
        {
            ChannelName = queueName;
            Client = new ServiceBusClient(connectionSettings.ConnectionString);
            Processor = Client.CreateProcessor(queueName,
                new ServiceBusProcessorOptions { AutoCompleteMessages = true, MaxConcurrentCalls = 1 });
        }
    }
}
