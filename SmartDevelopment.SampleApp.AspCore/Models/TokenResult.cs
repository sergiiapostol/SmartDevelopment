using Newtonsoft.Json;
using System;

namespace SmartDevelopment.SampleApp.AspCore.Models
{
    public class TokenResult
    {
        [JsonProperty("t")]
        public string AccessToken { get; set; }

        [JsonProperty("tt")]
        public string TokenType { get; set; }

        [JsonProperty("e")]
        public DateTime ExpiresAt { get; set; }

        public override string ToString()
        {
            return $"tt:{TokenType} e:{ExpiresAt} t:{AccessToken}";
        }
    }
}
