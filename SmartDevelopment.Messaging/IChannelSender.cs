using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartDevelopment.Messaging
{
    public interface IChannel
    {
        Task Init();
    }

    public interface IChannelSender<TMessage> : IChannel where TMessage : class
    {
        string ChannelName { get; }

        Task Add(TMessage message, TimeSpan? initialDelay = null);
        Task Add(List<TMessage> message, TimeSpan? initialDelay = null);
    }
}
