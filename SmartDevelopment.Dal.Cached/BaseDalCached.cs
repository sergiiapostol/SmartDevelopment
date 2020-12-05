using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartDevelopment.Caching.EnrichedMemoryCache;
using SmartDevelopment.Dal.Abstractions;
using SmartDevelopment.Dal.Abstractions.Models;
using SmartDevelopment.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SmartDevelopment.Dal.Cached
{
    public abstract class BaseDalCached<TEntity> : IDalCached<TEntity>
        where TEntity : class, IDbEntity
    {
        private readonly IDal<TEntity> _dal;
        private readonly IEnrichedMemoryCache _memoryCache;
        private readonly ILogger _logger;

        public BaseDalCached(IDal<TEntity> dal, IEnrichedMemoryCache memoryCache, ILogger logger)
        {
            _dal = dal;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        protected abstract string CacheKey { get; }

        protected abstract MemoryCacheEntryOptions CacheOptions { get; }

        protected string GetCacheKey(ObjectId id)
        {
            return $"{CacheKey}_{id}";
        }                            

        public Task<ITransaction> OpenTransaction()
        {
            return _dal.OpenTransaction();
        }

        #region Insert

        public Task<long> InsertAsync(TEntity entity)
        {
            return _dal.InsertAsync(entity);
        }

        public Task<long> InsertAsync(List<TEntity> entities)
        {
            return _dal.InsertAsync(entities);
        }

        public async Task<InsertOrUpdateResult> InsertOrUpdateAsync(List<TEntity> entities)
        {
            var result = await _dal.InsertOrUpdateAsync(entities).ConfigureAwait(false);

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

        #endregion

        #region Update

        public async Task<TEntity> UpdateAsync(ObjectId id, TEntity entity)
        {
            var result = await _dal.UpdateAsync(id, entity).ConfigureAwait(false);

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

        public async Task<long> UpdateAsync(IList<TEntity> entities)
        {
            var result = await _dal.UpdateAsync(entities).ConfigureAwait(false);

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

        public Task<long> SetAsync<TProperty>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProperty>> property, TProperty value, bool upsert = false)
        {
            return _dal.SetAsync(filter, property, value, upsert);
        }

        public async Task<long> SetAsync<TProperty>(ObjectId id, Expression<Func<TEntity, TProperty>> property, TProperty value, bool upsert = false)
        {
            var result = await _dal.SetAsync(id, property, value, upsert).ConfigureAwait(false);
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

        public Task<long> SetAsync(Expression<Func<TEntity, bool>> filter, List<PropertyUpdate<TEntity>> updates, bool upsert = false)
        {
            return _dal.SetAsync(filter, updates, upsert);
        }

        public async Task<long> SetAsync(ObjectId id, List<PropertyUpdate<TEntity>> updates, bool upsert = false)
        {
            var result = await _dal.SetAsync(id, updates, upsert).ConfigureAwait(false);
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

        public Task<long> IncrementProperty<TProperty>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProperty>> property, TProperty value)
        {
            return _dal.IncrementProperty(filter, property, value);
        }

        public async Task<long> IncrementProperty<TProperty>(ObjectId id, Expression<Func<TEntity, TProperty>> property, TProperty value)
        {
            var result = await _dal.IncrementProperty(id, property, value).ConfigureAwait(false);
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

        #endregion

        #region Delete

        public async Task DeleteAsync(ObjectId id)
        {
            await _dal.DeleteAsync(id).ConfigureAwait(false);

            try
            {
                await _memoryCache.Remove(GetCacheKey(id)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
            }
        }

        public async Task DeleteAsync(TEntity entity)
        {
            await _dal.DeleteAsync(entity).ConfigureAwait(false);

            try
            {
                await _memoryCache.Remove(GetCacheKey(entity.Id)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
            }
        }

        public Task<long> DeleteAsync(Expression<Func<TEntity, bool>> filter)
        {
            return _dal.DeleteAsync(filter);
        }

        #endregion

        #region Get

        public async Task<TEntity> GetAsync(ObjectId id)
        {
            var cachedEntity = await _memoryCache.GetOrAdd(GetCacheKey(id), () => _dal.GetAsync(id), CacheOptions, 
                new Dictionary<string, string> { { CacheKey, id.ToString()} }).ConfigureAwait(false);

            if (cachedEntity == null)
                return await _dal.GetAsync(id).ConfigureAwait(false);

            return cachedEntity;
        }

        public Task<List<TEntity>> GetAsync(PagingInfo pagingInfo, Expression<Func<TEntity, bool>> filter = null, SortingSettings<TEntity> orderBy = null)
        {
            return _dal.GetAsync(pagingInfo, filter, orderBy);
        }

        public Task<List<TEntity>> SearchAsync(PagingInfo pagingInfo, SearchSettings<TEntity> filter = null, SortingSettings<TEntity> orderBy = null)
        {
            return _dal.SearchAsync(pagingInfo, filter, orderBy);
        }

        public Task<long> CountAsync(SearchSettings<TEntity> filter)
        {
            return _dal.CountAsync(filter);
        }

        public Task<long> CountAsync(Expression<Func<TEntity, bool>> filter)
        {
            return _dal.CountAsync(filter);
        }

        #endregion

        public IQueryable<TEntity> AsQueryable()
        {
            return _dal.AsQueryable();
        }
    }
}