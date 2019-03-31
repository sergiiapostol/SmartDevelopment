using System;

namespace SmartDevelopment.HttpClientExtensions
{
    public class HttpRequestException : Exception
    {
        public HttpRequestException()         {
        }

        public HttpRequestException(string message) : base(message)
        {
        }

        public HttpRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public string StatusCode { get; set; }

        public string ReasonPhrase { get; set; }

        public string RawResponse { get; set; }

        public string Uri { get; set; }
    }
}
