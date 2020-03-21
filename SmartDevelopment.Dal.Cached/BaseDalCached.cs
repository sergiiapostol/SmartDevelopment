using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartDevelopment.Caching.EnrichedMemoryCache;
using SmartDevelopment.Dal.Abstractions;
using SmartDevelopment.Dal.Abstractions.Models;
using SmartDevelopment.Dal.MongoDb;
using SmartDevelopment.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartDevelopment.Dal.Cached
{
    public class BaseDalCached<TEntity> : BaseDal<TEntity>, IDalCached<TEntity>
        where TEntity : class, IDbEntity
    {
        private readonly IEnrichedMemoryCache _memoryCache;
        private readonly ILogger _logger;

        public BaseDalCached(IMongoDatabaseFactory databaseFactory, IEnrichedMemoryCache memoryCache, ILogger logger) :
            base(databaseFactory)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public virtual string CacheKey { get; }

        public Task<TEntity> GetCachedAsync(ObjectId id, MemoryCacheEntryOptions options)
        {
            return _memoryCache.GetOrAdd(GetCacheKey(id), () => base.GetAsync(id), options);
        }

        public override async Task<InsertOrUpdateResult> InsertOrUpdateAsync(List<TEntity> entities)
        {
            var result = await base.InsertOrUpdateAsync(entities).ConfigureAwait(false);

            try
            {
                await Task.WhenAll(entities.Select(v => _memoryCache.Remove(GetCacheKey(v.Id))).ToList()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
            }

            return result;
        }

        public override async Task<TEntity> UpdateAsync(ObjectId id, TEntity entity)
        {
            var result = await base.UpdateAsync(id, entity).ConfigureAwait(false);

            try
            {
                await _memoryCache.Remove(GetCacheKey(id)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
            }

            return result;
        }

        public override async Task<long> UpdateAsync(IList<TEntity> entities)
        {
            var result = await base.UpdateAsync(entities).ConfigureAwait(false);

            try
            {
                await Task.WhenAll(entities.Select(v => _memoryCache.Remove(GetCacheKey(v.Id))).ToList()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
            }

            return result;
        }

        protected override async Task SetAsync(ObjectId id, UpdateDefinition<TEntity> update, UpdateOptions options = null)
        {
            await base.SetAsync(id, update, options).ConfigureAwait(false);

            try
            {
                await _memoryCache.Remove(GetCacheKey(id)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
            }
        }

        public override async Task DeleteAsync(ObjectId id)
        {
            await base.DeleteAsync(id).ConfigureAwait(false);

            try
            {
                await _memoryCache.Remove(GetCacheKey(id)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
            }
        }

        private string GetCacheKey(ObjectId id)
        {
            return $"{CacheKey}_{id}";
        }
    }
}