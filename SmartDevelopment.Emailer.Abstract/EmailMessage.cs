using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartDevelopment.Emailer.Abstract
{
    public class EmailMessage
    {
        [JsonConstructor]
        private EmailMessage()
        {
        }

        protected EmailMessage(string from, string to)
        {
            From = from;
            To = to;
        }

        public string From { get; set; }

        public string To { get; set; }

        public string TemplateId { get; set; }

        public string TemplateData { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public string BodyMimeType { get; set; } = "text/plain";

        public override string ToString()
        {
            return $"f:{From} t:{To} s:{Subject} tId:{TemplateId}";
        }
    }

    public sealed class TemplatedEmailMessage : EmailMessage
    {
        public TemplatedEmailMessage(string from, string to, string templateId, object templateData)
            : base(from, to)
        {
            TemplateId = templateId;
            TemplateData = TemplateData = JsonSerializer.Serialize(templateData);
        }
    }

    public sealed class TextualEmailMessage : EmailMessage
    {
        public TextualEmailMessage(string from, string to, string subject, string body)
            : base(from, to)
        {
            Subject = subject;
            Body = body;
        }
    }
}
