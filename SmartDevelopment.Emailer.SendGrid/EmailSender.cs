using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using SmartDevelopment.Emailer.Abstract;
using SmartDevelopment.Logging;
using System;
using System.Threading.Tasks;

namespace SmartDevelopment.Emailer.SendGrid
{
    public sealed class EmailSender
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
                var response = await _sendGridClient.SendEmailAsync(emailRequest).ConfigureAwait(false);
                _logger.Debug($"SendGrid response {response.StatusCode}");
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

            if (string.IsNullOrWhiteSpace(email.TemplateId))
            {
                msg.AddContent(email.BodyMimeType, email.Body);
                msg.Subject = email.Subject;
            }
            else
            {
                msg.SetTemplateId(email.TemplateId);
                msg.SetTemplateData(JsonConvert.DeserializeObject(email.TemplateData));
            }

            return msg;
        }
    }
}
