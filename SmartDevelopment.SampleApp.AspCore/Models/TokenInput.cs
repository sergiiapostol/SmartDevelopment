using Newtonsoft.Json;

namespace SmartDevelopment.SampleApp.AspCore.Models
{
    public class TokenInput
    {
        [JsonRequired]
        [JsonProperty("e")]
        public string Email { get; set; }

        [JsonRequired]
        [JsonProperty("p")]
        public string Password { get; set; }

        public override string ToString()
        {
            return $"e:{Email} p:{Password}";
        }
    }
}
