using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using SmartDevelopment.Dal.Abstractions;
using System.Threading.Tasks;

namespace SmartDevelopment.Dal.Cached
{
    public interface IDalCached<TEntity>
        where TEntity : class, IDbEntity
    {
        Task<TEntity> GetCachedAsync(ObjectId id, MemoryCacheEntryOptions cacheOptions);

        string CacheKey { get; }
    }
}
