using System.Threading.Tasks;

namespace SmartDevelopment.Messaging
{
    public interface IChannelReceiver<TMessage> : IChannel
        where TMessage : class
    {
        string ChannelName { get; }

        Task ProcessMessage(TMessage message);
    }
}
