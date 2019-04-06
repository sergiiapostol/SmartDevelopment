using Microsoft.Extensions.DependencyInjection;
using SendGrid;

namespace SmartDevelopment.Emailer.SendGrid
{
    public static class SendGridEmailerServicesExtensions
    {
        public static IServiceCollection AddSendGridSender(this IServiceCollection services, SendGridClientOptions options)
        {
            services.AddSingleton(options);
            services.AddSingleton<ISendGridClient, SendGridClient>();
            services.AddSingleton<EmailSender>();

            return services;
        }
    }
}
