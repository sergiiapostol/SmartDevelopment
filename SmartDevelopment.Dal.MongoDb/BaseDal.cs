﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using SmartDevelopment.Dal.Abstractions;
using SmartDevelopment.Dal.Abstractions.Exceptions;
using SmartDevelopment.Dal.Abstractions.Models;

namespace SmartDevelopment.Dal.MongoDb
{
    public abstract class BaseDal<TEntity> : IMongoDbDal<TEntity>, IDisposable
        where TEntity : class, IDbEntity
    {
        protected readonly IMongoDatabaseFactory DatabaseFactory;

        protected BaseDal(IMongoDatabaseFactory databaseFactory)
        {
            DatabaseFactory = databaseFactory;
        }

        private IMongoCollection<TEntity> _collection;

        protected IMongoCollection<TEntity> Collection
        {
            get
            {
                return _collection ??= GetCollection();
            }
        }

        private IMongoCollection<TEntity> GetCollection()
        {
            var collectionName = typeof(TEntity).Name;

            return GetCollection<TEntity>(collectionName);
        }

        protected IMongoCollection<TEntity2> GetCollection<TEntity2>(string collectionName = null)
            where TEntity2 : IDbEntity
        {
            var db = DatabaseFactory.Get();
            collectionName ??= typeof(TEntity2).Name;
            return db.GetCollection<TEntity2>(collectionName);
        }

        public virtual void Dispose()
        {
        }

        protected static FilterDefinitionBuilder<TEntity> Filter => Builders<TEntity>.Filter;

        protected static UpdateDefinitionBuilder<TEntity> Update => Builders<TEntity>.Update;

        protected static IndexKeysDefinitionBuilder<TEntity> IndexKeys => Builders<TEntity>.IndexKeys;

        public async Task<ITransaction> OpenTransaction()
        {
            var session = await DatabaseFactory.Get().Client.StartSessionAsync();
            session.StartTransaction();
            return new Transaction(session);
        }

        private class Transaction : ITransaction
        {
            private readonly IClientSessionHandle _session;

            internal Transaction(IClientSessionHandle session)
            {
                _session = session;
            }

            public Task Commit()
            {
                return _session.CommitTransactionAsync();
            }            

            public Task Rollback()
            {
                return _session.AbortTransactionAsync();
            }

            public void Dispose()
            {
                _session.Dispose();
            }
        }

        #region Insert

        public async Task<long> InsertAsync(TEntity entity)
        {
            if (entity == null)
                throw new NullReferenceException("Entity is required");

            try
            {
                await Collection.InsertOneAsync(entity);
            }
            catch (MongoWriteException ex) when (ex.InnerException is MongoBulkWriteException<TEntity>)
            {
                var inserted = ((MongoBulkWriteException<TEntity>)ex.InnerException).Result;
                throw new InsertOrUpdateException(new InsertOrUpdateResult(inserted.InsertedCount, 0), ex);
            }

            return 1;
        }

        public async Task<long> InsertAsync(List<TEntity> entities)
        {
            if (entities == null)
                throw new NullReferenceException("Entities is required");

            if (entities.Count > 0)
            {
                try
                {
                    await Collection.InsertManyAsync(entities, new InsertManyOptions { IsOrdered = false });
                }
                catch (MongoWriteException ex) when (ex.InnerException is MongoBulkWriteException<TEntity>)
                {
                    var inserted = ((MongoBulkWriteException<TEntity>)ex.InnerException).Result;
                    throw new InsertOrUpdateException(new InsertOrUpdateResult(inserted.InsertedCount, 0), ex);
                }
            }

            return entities.Count;
        }

        public virtual async Task<InsertOrUpdateResult> InsertOrUpdateAsync(List<TEntity> entities)
        {
            if (entities == null)
                throw new NullReferenceException("Entities is required");
            if (entities.Count > 0)
            {
                try
                {
                    var upsertOperations = entities.Where(v => v.Id != ObjectId.Empty).Select(
                        v => new ReplaceOneModel<TEntity>(Filter.Eq(c => c.Id, v.Id), v) { IsUpsert = true }).Cast<WriteModel<TEntity>>();
                    var insertIperations =
                        entities.Where(v => v.Id == ObjectId.Empty).Select(v => new InsertOneModel<TEntity>(v)).Cast<WriteModel<TEntity>>();
                    var result = await
                        Collection.BulkWriteAsync(upsertOperations.Union(insertIperations).ToList(),
                            new BulkWriteOptions { IsOrdered = false }, CancellationToken.None);

                    return new InsertOrUpdateResult(result.InsertedCount, result.ModifiedCount);
                }
                catch (MongoBulkWriteException<TEntity> ex)
                {
                    throw new InsertOrUpdateException(
                        new InsertOrUpdateResult(ex.Result.InsertedCount, ex.Result.ModifiedCount), ex);
                }
            }
            return new InsertOrUpdateResult(0, 0);
        }

        #endregion

        #region Update

        public virtual async Task<TEntity> UpdateAsync(ObjectId id, TEntity entity)
        {
            if (entity == null)
                throw new NullReferenceException("Entity is required");

            entity.ModifiedAt = DateTime.UtcNow;

            var result =
                await
                    Collection.ReplaceOneAsync(v => v.Id.Equals(id), entity, new ReplaceOptions { IsUpsert = true });

            if (result.IsAcknowledged && result.MatchedCount == 0)
                throw new EntityNotFoundException(typeof(TEntity), id);

            return entity;
        }

        public virtual async Task<TEntity> UpsertAsync(ObjectId id, TEntity entity)
        {
            if (entity == null)
                throw new NullReferenceException("Entity is required");

            entity.ModifiedAt = DateTime.UtcNow;

            var result =
                await
                    Collection.ReplaceOneAsync(v => v.Id.Equals(id), entity, new ReplaceOptions { IsUpsert = true });

            return entity;
        }

        public virtual async Task<long> UpdateAsync(IList<TEntity> entities)
        {
            if (entities == null)
                throw new NullReferenceException("Entities is required");

            if (entities.Count == 0)
                return 0;

            foreach (var item in entities)
            {
                item.ModifiedAt = DateTime.UtcNow;
            }

            var result = await Collection.BulkWriteAsync(
                entities.Select(v => new ReplaceOneModel<TEntity>(Filter.Eq(f => f.Id, v.Id), v)),
                new BulkWriteOptions { IsOrdered = false });

            if (result.IsAcknowledged && result.MatchedCount != entities.Count)
                throw new EntityNotFoundException(typeof(TEntity), string.Join(",", entities.Select(v => v.Id)));

            return result.ModifiedCount;
        }

        public Task<long> SetAsync<TProperty>(Expression<Func<TEntity, bool>> filter,
            Expression<Func<TEntity, TProperty>> property, TProperty value, bool upsert = false)
        {
            return SetAsync(Filter.Where(filter), Update.Set(property, value), new UpdateOptions { IsUpsert = upsert} );
        }

        public Task<long> SetAsync<TProperty>(ObjectId id,
            Expression<Func<TEntity, TProperty>> property, TProperty value, bool upsert = false)
        {
            return SetAsync(id, Update.Set(property, value), new UpdateOptions { IsUpsert = upsert });
        }

        private UpdateDefinition<TEntity> CreateUpdateDefinition(List<PropertyUpdate<TEntity>> updates)
        {
            if (updates.Count == 0)
                return null;

            var sets = Update.Set(updates[0].Property, updates[0].Value);
            foreach (var update in updates.Skip(1))
            {
                sets = sets.Set(update.Property, update.Value);
            }

            return sets;
        }

        public Task<long> SetAsync(Expression<Func<TEntity, bool>> filter, List<PropertyUpdate<TEntity>> updates, bool upsert = false)
        {
            if (updates?.Count < 1)
                return Task.FromResult(0L);

            return SetAsync(Filter.Where(filter), CreateUpdateDefinition(updates), new UpdateOptions { IsUpsert = upsert });
        }

        public Task<long> SetAsync(ObjectId id, List<PropertyUpdate<TEntity>> updates, bool upsert = false)
        {
            if (updates?.Count < 1)
                return Task.FromResult(0L);

            return SetAsync(id, CreateUpdateDefinition(updates), new UpdateOptions { IsUpsert = upsert });
        }

        protected async Task<long> SetAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update,
            UpdateOptions options = null)
        {
            var result = await Collection.UpdateManyAsync(filter, update.Set(v => v.ModifiedAt, DateTime.UtcNow), options);
            return result.ModifiedCount + (result.UpsertedId != null ? 1 : 0);
        }

        protected virtual Task<long> SetAsync(ObjectId id, UpdateDefinition<TEntity> update,
            UpdateOptions options = null)
        {
            return SetAsync(Filter.Eq(v => v.Id, id), update, options);
        }

        public Task<long> IncrementProperty<TProperty>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProperty>> property, TProperty value)
        {
            return SetAsync(filter, Update.Inc(property, value));
        }

        public Task<long> IncrementProperty<TProperty>(ObjectId id, Expression<Func<TEntity, TProperty>> property, TProperty value)
        {
            return SetAsync(id, Update.Inc(property, value));
        }

        #endregion

        #region Delete

        public Task DeleteAsync(TEntity entity)
        {
            return DeleteAsync(entity.Id);
        }

        public virtual async Task DeleteAsync(ObjectId id)
        {
            var result = await Collection.DeleteOneAsync(v => v.Id.Equals(id));

            if (result.IsAcknowledged && result.DeletedCount == 0)
                throw new EntityNotFoundException(typeof(TEntity), id);
        }

        public Task<long> DeleteAsync(Expression<Func<TEntity, bool>> filter)
        {
            return DeleteAsync(BuildFilter(filter));
        }

        protected async Task<long> DeleteAsync(FilterDefinition<TEntity> filter)
        {
            var result = await Collection.DeleteManyAsync(filter);
            return result.DeletedCount;
        }

        #endregion

        #region Get

        public virtual async Task<TEntity> GetAsync(ObjectId id)
        {
            var result = await GetAsync(new PagingInfo(0, 1), Filter.Eq(v => v.Id, id));
            return result.FirstOrDefault();
        }

        protected virtual Task<List<TEntity>> GetAsync(PagingInfo pagingInfo,
            FilterDefinition<TEntity> filter = null,
            SortDefinition<TEntity> orderBy = null)
        {
            var query = Collection.Find(filter ?? FilterDefinition<TEntity>.Empty);

            if (orderBy != null)
            {
                query = query.Sort(orderBy);
            }

            if (pagingInfo != null)
            {
                query = query.Skip(pagingInfo.Skip).Limit(pagingInfo.Take);
            }
            return query.ToListAsync();
        }

        public virtual Task<List<TEntity>> GetAsync(PagingInfo pagingInfo,
            Expression<Func<TEntity, bool>> filter = null,
            SortingSettings<TEntity> orderBy = null)
        {
            var query = BuildFilter(filter);
            var sort = BuildSorting(orderBy);

            return GetAsync(pagingInfo, query, sort);
        }

        protected virtual FilterDefinition<TEntity> BuildSearchFilter(SearchSettings<TEntity> filter)
        {
            var query = BuildFilter(filter.Filter);
            if (!string.IsNullOrEmpty(filter.Search))
            {
                var searchText = filter.FullMatch ? BildTextFullMatchFilter(filter.Search) : filter.Search;
                query = Filter.And(Filter.Text(searchText), query);
            }

            return query;
        }

        protected string BildTextFullMatchFilter(string text)
        {
            var tokens = text.Split(' ');
            return string.Join(" ", tokens.Select(v => '"' + v + '"'));
        }

        public virtual Task<List<TEntity>> SearchAsync(PagingInfo pagingInfo, SearchSettings<TEntity> filter, SortingSettings<TEntity> orderBy = null)
        {
            var query = BuildSearchFilter(filter);
            var sort = BuildSorting(orderBy);

            return GetAsync(pagingInfo, query, sort);
        }

        public Task<long> CountAsync(SearchSettings<TEntity> filter)
        {
            var query = BuildSearchFilter(filter);

            return Collection.CountDocumentsAsync(query);
        }

        public Task<long> CountAsync(Expression<Func<TEntity, bool>> filter)
        {
            return Collection.CountDocumentsAsync(BuildFilter(filter));
        }

        public IQueryable<TEntity> AsQueryable()
        {
            return ((IMongoDbDal<TEntity>)this).AsMongoDbQueryable();
        }

        IQueryable<TEntity> IMongoDbDal<TEntity>.AsMongoDbQueryable()
        {
            return Collection.AsQueryable();
        }

        #endregion

        #region Aggregation

        protected Task<List<TAggregatedEntity>> ExecuteAsync<TAggregatedEntity>(
            IAggregateFluent<TAggregatedEntity> query)
        {
            return query.ToListAsync();
        }

        #endregion

        #region Helpers

        protected virtual FilterDefinition<TEntity> BuildFilter(Expression<Func<TEntity, bool>> filter = null)
        {
            if (filter == null)
                return FilterDefinition<TEntity>.Empty;
            return Filter.Where(filter);
        }

        protected static SortDefinition<TSortedEntity> BuildSorting<TSortedEntity>(
            SortingSettings<TSortedEntity> orderBy)
            where TSortedEntity : TEntity
        {
            var builder = Builders<TSortedEntity>.Sort;

            if (orderBy == null)
                return null;

            var sorts = new List<SortDefinition<TSortedEntity>>(orderBy.Items.Count);
            foreach (var sortingSetting in orderBy.Items)
            {
                var sort = sortingSetting.Asc
                    ? Builders<TSortedEntity>.Sort.Ascending(sortingSetting.Expression)
                    : Builders<TSortedEntity>.Sort.Descending(sortingSetting.Expression);

                sorts.Add(sort);
            }
            return builder.Combine(sorts.Where(v => v != null));
        }

        #endregion
    }
}