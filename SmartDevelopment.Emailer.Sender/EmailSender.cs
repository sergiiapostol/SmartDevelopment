using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using SmartDevelopment.Emailer.Abstract;
using SmartDevelopment.Logging;
using System;
using System.Threading.Tasks;

namespace SmartDevelopment.Emailer.Sender
{
    public sealed class EmailSender : IEmailSender
    {
        private readonly ILogger _logger;

        private readonly ISendGridClient _sendGridClient;

        public EmailSender(ISendGridClient sendGridClient, ILogger<EmailSender> logger)
        {
            _logger = logger;
            _sendGridClient = sendGridClient;
        }

        public async Task Send(EmailMessage email)
        {
            SendGridMessage emailRequest;
            try
            {
                emailRequest = BuildMessage(email);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
                return;
            }

            try
            {
                await _sendGridClient.SendEmailAsync(emailRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
                throw;
            }
        }

        private SendGridMessage BuildMessage(EmailMessage email)
        {
            var msg = new SendGridMessage { From = new EmailAddress(email.From) };
            msg.AddTo(email.To);

            if(!string.IsNullOrEmpty(email.TemplateId))
            {
                msg.SetTemplateId(email.TemplateId);
                var templateData = JsonConvert.DeserializeObject(email.TemplateData);
                msg.SetTemplateData(templateData);
            }
            else
            {
                msg.SetSubject(email.Subject);
                msg.PlainTextContent = email.Body;
            }

            return msg;
        }
    }
}
