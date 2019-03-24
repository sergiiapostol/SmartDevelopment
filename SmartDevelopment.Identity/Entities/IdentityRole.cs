using MongoDB.Bson;
using SmartDevelopment.Dal.Abstractions;
using System;

namespace SmartDevelopment.Identity.Entities
{
	public class IdentityRole : Microsoft.AspNetCore.Identity.IdentityRole<ObjectId>, IDbEntity
	{
		public IdentityRole()
		{
		    Id = ObjectId.GenerateNewId();
		}

        public DateTime? ModifiedAt { get; set; }

        public string Serialize()
	    {
	        return ToString();
	    }
    }
}