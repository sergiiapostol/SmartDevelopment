using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System;

namespace SmartDevelopment.Dal.MongoDb
{
    public interface IMongoDatabaseFactory
    {
        IMongoDatabase Get();
    }

    public class MongoDatabaseFactory : IMongoDatabaseFactory
    {
        private readonly IMongoClientFactory _clientFactory;
        private readonly ConnectionSettings _connectionSettings;

        private IMongoDatabase _database;

        private readonly object _lock = new object();

        public MongoDatabaseFactory(ConnectionSettings connectionSettings, IMongoClientFactory clientFactory)
        {
            _connectionSettings = connectionSettings;
            _clientFactory = clientFactory;
            BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
            BsonSerializer.RegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
            BsonSerializer.RegisterSerializer(typeof(DateTime), new DateTimeSerializer(DateTimeKind.Utc));
            BsonSerializer.RegisterSerializer(typeof(DateTime?), new NullableSerializer<DateTime>(new DateTimeSerializer(DateTimeKind.Utc)));
        }

        public IMongoDatabase Get()
        {
            if (_database == null)
            {
                lock (_lock)
                {
                    if (_database == null)
                    {
                        var connectionString = new MongoUrl(_connectionSettings.ConnectionString);
                        var client = _clientFactory.GetClient(connectionString);

                        _database = client.GetDatabase(connectionString.DatabaseName,
                            new MongoDatabaseSettings { GuidRepresentation = GuidRepresentation.CSharpLegacy });
                    }
                }
            }

            return _database;
        }
    }
}