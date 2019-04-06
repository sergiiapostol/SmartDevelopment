using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Logging.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace SmartDevelopment.Jobs.Base
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder ConfigureJobHost(this IHostBuilder hostBuilder)
        {
            return hostBuilder
            .ConfigureWebJobs(b =>
             {
                 b.AddAzureStorageCoreServices()
                 .AddAzureStorage()
                 .AddTimers();
             })
            .UseConsoleLifetime();
        }

        public static IHostBuilder ConfigureAppConfiguration(this IHostBuilder hostBuilder, string envVariableName)
        {
            return hostBuilder.ConfigureAppConfiguration((_, config) =>
            {
                var environment = config.AddEnvironmentVariables().Build().GetValue<string>(envVariableName);
                config.AddJsonFile($"appsettings.json", optional: false, reloadOnChange: false);
                config.AddJsonFile($"appsettings.{environment}.json", optional: false, reloadOnChange: false);
            });
        }

        public static IHostBuilder ConfigureLogging(this IHostBuilder hostBuilder, string appInsightKeyName, string loggingConfigurationName)
        {
            return hostBuilder.ConfigureLogging((context, b) =>
            {
                b.SetMinimumLevel(LogLevel.Debug);
                b.AddConfiguration(context.Configuration.GetSection(loggingConfigurationName));
                b.AddConsole();

                var appInsightKey = context.Configuration.GetValue<string>(appInsightKeyName);
                b.AddApplicationInsights(o =>
                {
                    o.InstrumentationKey = appInsightKey;
                });
            });
        }

        public static IHostBuilder ConfigureQueueListener(this IHostBuilder hostBuilder, int batchSize, TimeSpan pollingInterval, TimeSpan visibilityTimeout)
        {
            return hostBuilder.ConfigureServices((_, services) =>
            {
                services.Configure((QueuesOptions o) =>
                {
                    o.MaxPollingInterval = pollingInterval;
                    o.BatchSize = batchSize;
                    o.VisibilityTimeout = visibilityTimeout;
                });
            });
        }

        public static IHostBuilder ConfigureJobActivator(this IHostBuilder hostBuilder, Func<IConfiguration, IServiceCollection, IServiceProvider> containerBuilder)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                var container = containerBuilder.Invoke(context.Configuration, services);
                var activator = new Activator(container);
                services.AddSingleton<IJobActivator>(_ => activator);
            });

            return hostBuilder;
        }
    }
}