using SmartDevelopment.Dal.Abstractions;
using System.Linq;

namespace SmartDevelopment.Dal.MongoDb
{
    public interface IMongoDbDal<TEntity> : IDal<TEntity>
        where TEntity : class, IDbEntity
    {
        IQueryable<TEntity> AsMongoDbQueryable();
    }
}
