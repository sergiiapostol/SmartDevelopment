﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using SmartDevelopment.Emailer.Abstract;
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
                _logger.LogError(ex, $"Failed to build email object");
                return;
            }

            try
            {
                var response = await _sendGridClient.SendEmailAsync(emailRequest);
                _logger.LogDebug($"SendGrid response {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email");
                throw;
            }
        }

        private static SendGridMessage BuildMessage(EmailMessage email)
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
