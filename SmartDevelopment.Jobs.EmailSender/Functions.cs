using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SmartDevelopment.Emailer.Abstract;
using SmartDevelopment.Jobs.Base;
using System;
using System.Threading.Tasks;

namespace SmartDevelopment.Jobs.EmailSender
{
    [ErrorHandler]
    public class Functions
    {
        private readonly Emailer.SendGrid.EmailSender _sender;
        private readonly Logging.ILogger<Functions> _logger;

        public Functions(Emailer.SendGrid.EmailSender sender, Logging.ILogger<Functions> logger)
        {
            _sender = sender;
            _logger = logger;
        }

        [FunctionName("ProcessEmail")]
        public async Task ProcessEmail([QueueTrigger("emails")] EmailMessage email, ILogger log)
        {
            log.LogInformation($"{nameof(ProcessEmail)} for {email} started at: {DateTime.UtcNow}");

            await _sender.Send(email);

            log.LogInformation($"{nameof(ProcessEmail)} for {email} completed at: {DateTime.UtcNow}");
        }

    }
}