using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using SmartDevelopment.Dal.Abstractions.Models;

namespace SmartDevelopment.Dal.Abstractions
{
    public interface IDal<TEntity> : IDalMarker
        where TEntity : IDbEntity
    {
        Task<long> InsertAsync(TEntity entity);
        Task<long> InsertAsync(List<TEntity> entities);
        Task<InsertOrUpdateResult> InsertOrUpdateAsync(List<TEntity> entities);

        Task<TEntity> UpdateAsync(ObjectId id, TEntity entity);
        Task<long> UpdateAsync(IList<TEntity> entities);

        Task<long> SetAsync<TProperty>(Expression<Func<TEntity, bool>> filter,
            Expression<Func<TEntity, TProperty>> property, TProperty value);

        Task DeleteAsync(TEntity entity);
        Task DeleteAsync(ObjectId id);
        Task<long> DeleteAsync(Expression<Func<TEntity, bool>> filter);

        Task<TEntity> GetAsync(ObjectId id);

        Task<List<TEntity>> GetAsync(PagingInfo pagingInfo,
            Expression<Func<TEntity, bool>> filter = null,
            SortingSettings<TEntity> orderBy = null);

        Task<List<TEntity>> SearchAsync(PagingInfo pagingInfo,
            SearchSettings<TEntity> filter = null,
            SortingSettings<TEntity> orderBy = null);

        Task<long> CountAsync(SearchSettings<TEntity> filter);
        Task<long> CountAsync(Expression<Func<TEntity, bool>> filter);

        IQueryable<TEntity> AsQueryable();
    }
}