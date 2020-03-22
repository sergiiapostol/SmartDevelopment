using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using SmartDevelopment.Caching.EnrichedMemoryCache;
using SmartDevelopment.Dal.Abstractions;
using SmartDevelopment.Dal.Cached;
using SmartDevelopment.Dal.MongoDb;
using SmartDevelopment.Logging;
using System;

namespace SmartDevelopment.SampleApp.AspCore.Dals
{
    public class TestEntity : IDbEntity
    {
        public ObjectId Id { get; set; }

        public DateTime? ModifiedAt { get; set; }

        public string Text { get; set; }
    }

    public class TestDal : BaseDal<TestEntity>
    {
        public TestDal(IMongoDatabaseFactory databaseFactory) : base(databaseFactory)
        {
        }
    }

    public class TestDalCached : BaseDalCached<TestEntity>
    {
        public TestDalCached(IDal<TestEntity> dal, IEnrichedMemoryCache memoryCache, ILogger logger) : base(dal, memoryCache, logger)
        {
        }

        protected override string CacheKey => "Test";

        protected override MemoryCacheEntryOptions CacheOptions => new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(30)};
    }
}
