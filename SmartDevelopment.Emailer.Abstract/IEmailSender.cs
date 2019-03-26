using System.Threading.Tasks;

namespace SmartDevelopment.Emailer.Abstract
{
    public interface IEmailSender
    {
        Task Send(EmailMessage message);
    }
}
