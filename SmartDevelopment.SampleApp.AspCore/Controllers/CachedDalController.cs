using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using SmartDevelopment.Caching.EnrichedMemoryCache;
using SmartDevelopment.Dal.Abstractions;
using SmartDevelopment.Dal.Abstractions.Models;
using SmartDevelopment.Dal.Cached;
using SmartDevelopment.Dal.MongoDb;
using SmartDevelopment.Logging;

namespace SmartDevelopment.SampleApp.AspCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class CachedDalController : ControllerBase
    {
        private readonly Random _random;
        private readonly IDalCached<TestEntity> _dal;

        public CachedDalController(IDalCached<TestEntity> dal)
        {
            _dal = dal;
            _random = new Random();
        }

        [HttpPost]
        public async Task<IActionResult> Create()
        {
            var entity = new TestEntity { Text = "1" };
            await _dal.InsertAsync(entity).ConfigureAwait(false);
            entity = await _dal.GetAsync(entity.Id).ConfigureAwait(false);

            await _dal.SetAsync(entity.Id, v => v.Text, "2").ConfigureAwait(false);
            entity = await _dal.GetAsync(entity.Id).ConfigureAwait(false);

            await _dal.SetAsync(entity.Id, 
                new List<PropertyUpdate<TestEntity>> { new PropertyUpdate<TestEntity>(v=>v.Text, "3") }).ConfigureAwait(false);
            entity = await _dal.GetAsync(entity.Id).ConfigureAwait(false);

            entity.Text = "4";
            await _dal.UpdateAsync(entity.Id, entity).ConfigureAwait(false);
            entity = await _dal.GetAsync(entity.Id).ConfigureAwait(false);

            return Ok();
        }
    }

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
        public TestDalCached(IDal<TestEntity> dal, IEnrichedMemoryCache memoryCache, ILogger<TestDalCached> logger) : base(dal, memoryCache, logger)
        {
        }

        protected override string CacheKey => "Test";

        protected override MemoryCacheEntryOptions CacheOptions => new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(30) };
    }
}