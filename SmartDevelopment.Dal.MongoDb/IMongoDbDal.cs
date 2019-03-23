using MongoDB.Driver.Linq;
using SmartDevelopment.Dal.Abstractions;

namespace SmartDevelopment.Dal.MongoDb
{
    public interface IMongoDbDal<TEntity> : IDal<TEntity>
        where TEntity : class, IDbEntity
    {
        IMongoQueryable<TEntity> AsMongoDbQueryable();
    }
}
