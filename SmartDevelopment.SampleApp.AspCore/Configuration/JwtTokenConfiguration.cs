using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace SmartDevelopment.SampleApp.AspCore.Configuration
{
    public class JwtTokenConfiguration
    {
        public string SignKey { get; set; }

        public SymmetricSecurityKey SecurityKey =>
            new(Encoding.UTF8.GetBytes(SignKey));

        public string Audience { get; set; }

        public string Issuer { get; set; }

        public long ExpireInSec { get; set; }
    }
}