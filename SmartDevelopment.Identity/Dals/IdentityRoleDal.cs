using MongoDB.Driver;
using SmartDevelopment.Dal.Abstractions;
using SmartDevelopment.Dal.MongoDb;
using SmartDevelopment.Identity.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartDevelopment.Identity.Dals
{
    public class IdentityRoleDal<TRole> : BaseDal<TRole>, IIndexedSource
        where TRole : IdentityRole
    {
        public IdentityRoleDal(IMongoDatabaseFactory databaseFactory) : base(databaseFactory)
        {
        }

        public Task EnsureIndex()
        {
            return Collection.Indexes.CreateManyAsync(new List<CreateIndexModel<TRole>>
            {
                new CreateIndexModel<TRole>(
                    Builders<TRole>.IndexKeys
                        .Descending(v => v.NormalizedName),
                    new CreateIndexOptions<TRole>
                    {
                        Unique = true,
                        Background = true
                    })
            });
        }
    }
}
