using MongoDB.Bson;
using System;

namespace SmartDevelopment.Dal.Abstractions
{
    public interface IDbEntity
    {
        ObjectId Id { get; set; }

        DateTime? ModifiedAt { get; set; }
    }
}
