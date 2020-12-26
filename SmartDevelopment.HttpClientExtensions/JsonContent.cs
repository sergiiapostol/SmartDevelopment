using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SmartDevelopment.HttpClientExtensions
{
    public class JsonContent : StringContent
    {
        public JsonContent(string content) : base(content, Encoding.UTF8, "application/json")
        {
        }

        public static JsonContent Create(object content)
        {
            var json = JsonSerializer.Serialize(content);
            return new JsonContent(json);
        }
    }
}
