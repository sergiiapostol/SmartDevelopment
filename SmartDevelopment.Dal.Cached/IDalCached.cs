using SmartDevelopment.Dal.Abstractions;

namespace SmartDevelopment.Dal.Cached
{
    public interface IDalCached<TEntity> : IDal<TEntity>
        where TEntity : class, IDbEntity
    {
    }
}
