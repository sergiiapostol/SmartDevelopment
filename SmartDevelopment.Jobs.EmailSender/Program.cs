using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SendGrid;
using SmartDevelopment.Emailer.SendGrid;
using SmartDevelopment.Jobs.Base;
using SmartDevelopment.Logging;
using System;

namespace SmartDevelopment.Jobs.EmailSender
{
    class Program
    {
        static void Main(string[] args)
        {
            var hostBuilder = new HostBuilder()
               .ConfigureJobHost()
               .ConfigureAppConfiguration("Environment")
               .ConfigureLogging("AppinsightKey", "Logging")
               .ConfigureQueueListener(10, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(1))
               .ConfigureJobActivator((configuration, services) =>
                {
                    services.AddSendGridSender(configuration.GetSection("SendGrid").Get<SendGridClientOptions>());

                    services.AddLogger(configuration.GetSection("LoggerSettings").Get<LoggerSettings>());

                    services.AddSingleton<Functions>();

                    return services.BuildServiceProvider();
                });

            var host = hostBuilder.Build();

            using (host)
            {
                host.Run();
            }
        }
    }
}