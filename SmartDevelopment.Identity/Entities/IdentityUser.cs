using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SmartDevelopment.Dal.Abstractions;
using System;
using System.Collections.Generic;

namespace SmartDevelopment.Identity.Entities
{
    [BsonIgnoreExtraElements]
    public class IdentityUser : IdentityUser<ObjectId>, IDbEntity
    {
        public List<string> Roles { get; set; } = new List<string>();

        public List<IdentityUserLogin<ObjectId>> Logins { get; set; } = new List<IdentityUserLogin<ObjectId>>();

        public List<IdentityUserClaim<ObjectId>> Claims { get; set; } = new List<IdentityUserClaim<ObjectId>>();

        public List<IdentityUserToken<ObjectId>> Tokens { get; set; } = new List<IdentityUserToken<ObjectId>>();

        [BsonIgnoreIfNull]
        public DateTime? ModifiedAt { get; set; }

        [BsonIgnoreIfNull]
        public DateTime? CreatedAt { get; set; }

        public int State { get; set; }
    }
}