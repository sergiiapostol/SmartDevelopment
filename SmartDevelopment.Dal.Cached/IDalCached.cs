using MongoDB.Bson;
using SmartDevelopment.Dal.Abstractions;
using System.Threading.Tasks;

namespace SmartDevelopment.Dal.Cached
{
    public interface IDalCached<TEntity> : IDal<TEntity>
        where TEntity : class, IDbEntity
    {
        Task<TEntity> GetCachedAsync(ObjectId id);
    }
}
