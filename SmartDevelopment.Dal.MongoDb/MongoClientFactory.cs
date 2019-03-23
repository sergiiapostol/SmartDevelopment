using MongoDB.Driver;
using System;

namespace SmartDevelopment.Dal.MongoDb
{
    public interface IMongoClientFactory
    {
        MongoClientSettings Get(MongoUrl mongoUrl);
        MongoClient GetClient(MongoUrl mongoUrl);
    }

    public class MongoClientFactory : IMongoClientFactory
    {
        public virtual MongoClientSettings Get(MongoUrl mongoUrl)
        {
            if (mongoUrl == null)
                throw new ArgumentException($"{nameof(mongoUrl)} shoould not be empty");

            return MongoClientSettings.FromUrl(mongoUrl);
        }

        public virtual MongoClient GetClient(MongoUrl mongoUrl)
        {
            return new MongoClient(Get(mongoUrl));
        }
    }
}