using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace SmartDevelopment.HttpClientExtensions
{
    public class JsonContent : StringContent
    {
        public JsonContent(string content) : base(content, Encoding.UTF8, "application/json")
        {
        }

        public static JsonContent Create(object content)
        {
            var json = JsonConvert.SerializeObject(content);
            return new JsonContent(json);
        }
    }
}
