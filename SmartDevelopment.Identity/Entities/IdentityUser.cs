using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using MongoDB.Bson;
using Microsoft.AspNetCore.Identity;
using SmartDevelopment.Dal.Abstractions;
using System;

namespace SmartDevelopment.Identity.Entities
{
    public class IdentityUser : IdentityUser<ObjectId>, IDbEntity
    {
        public IdentityUser()
        {
            Claims = new List<IdentityUserClaim>();
            Tokens = new List<IdentityUserToken>();
            Logins = new List<IdentityUserLogin>();
            Roles = new List<string>();
        }

		public List<string> Roles { get; set; }

		public void AddRole(string role)
		{
			Roles.Add(role);
		}

		public void RemoveRole(string role)
		{
			Roles.Remove(role);
		}

		public List<IdentityUserLogin> Logins { get; set; }

		public void AddLogin(UserLoginInfo login)
		{
			Logins.Add(new IdentityUserLogin(login));
		}

		public void RemoveLogin(string loginProvider, string providerKey)
		{
			Logins.RemoveAll(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey);
		}

		public List<IdentityUserClaim> Claims { get; set; }

		public void AddClaim(Claim claim)
		{
			Claims.Add(new IdentityUserClaim(claim));
		}

		public void RemoveClaim(Claim claim)
		{
			Claims.RemoveAll(c => c.Type == claim.Type && c.Value == claim.Value);
		}

		public void ReplaceClaim(Claim existingClaim, Claim newClaim)
		{
			var claimExists = Claims
				.Any(c => c.Type == existingClaim.Type && c.Value == existingClaim.Value);
			if (claimExists)
			{
				// note: nothing to update, ignore, no need to throw
				return;
			}
			RemoveClaim(existingClaim);
			AddClaim(newClaim);
		}

		public List<IdentityUserToken> Tokens { get; set; }

        private IdentityUserToken GetToken(string loginProider, string name)
			=> Tokens
				.Find(t => t.LoginProvider == loginProider && t.Name == name);

		public void SetToken(string loginProider, string name, string value)
		{
			var existingToken = GetToken(loginProider, name);
			if (existingToken != null)
			{
				existingToken.Value = value;
				return;
			}

			Tokens.Add(new IdentityUserToken
			{
				LoginProvider = loginProider,
				Name = name,
				Value = value
			});
		}

		public string GetTokenValue(string loginProider, string name)
		{
			return GetToken(loginProider, name)?.Value;
		}

		public void RemoveToken(string loginProvider, string name)
		{
			Tokens.RemoveAll(t => t.LoginProvider == loginProvider && t.Name == name);
		}

		public override string ToString() => UserName;

        public string Serialize()
        {
            return ToString();
        }

        public DateTime? ModifiedAt { get; set; }

        public int?  ResetToken { get; set; }
    }
}