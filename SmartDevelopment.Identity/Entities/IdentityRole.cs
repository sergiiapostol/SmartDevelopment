﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using SmartDevelopment.Dal.Abstractions;

namespace SmartDevelopment.Identity.Entities
{
    public class IdentityRole : IdentityRole<ObjectId>, IDbEntity
    {
        public List<IdentityRoleClaim<ObjectId>> Claims { get; set; } = new List<IdentityRoleClaim<ObjectId>>();

        public DateTime? ModifiedAt { get; set; }
    }
}
