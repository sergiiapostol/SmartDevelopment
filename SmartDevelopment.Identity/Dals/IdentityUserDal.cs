using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using SmartDevelopment.Dal.Abstractions;
using SmartDevelopment.Dal.MongoDb;
using SmartDevelopment.Identity.Entities;

namespace SmartDevelopment.Identity.Dals
{
    public class IdentityUserDal<TUser> : BaseDal<TUser>, IIndexedSource
        where TUser : IdentityUser
    {
        public IdentityUserDal(IMongoDatabaseFactory databaseFactory) : base(databaseFactory)
        {
        }

        public Task EnsureIndex()
        {
            return Collection.Indexes.CreateManyAsync(new List<CreateIndexModel<TUser>>
            {
                new CreateIndexModel<TUser>(
                    Builders<TUser>.IndexKeys
                        .Descending(v => v.NormalizedUserName),
                    new CreateIndexOptions<TUser>
                    {
                        Background = true, Unique = true
                    }),
                new CreateIndexModel<TUser>(
                    Builders<TUser>.IndexKeys
                        .Descending(v => v.NormalizedEmail),
                    new CreateIndexOptions<TUser>
                    {
                        Background = true, Sparse = true
                    }),
                new CreateIndexModel<TUser>(
                    Builders<TUser>.IndexKeys
                        .Descending("Logins.LoginProvider")
                        .Descending("Logins.ProviderKey"),
                    new CreateIndexOptions<TUser>
                    {
                        Background = true, Unique = true, Sparse = true
                    }),
                new CreateIndexModel<TUser>(
                    Builders<TUser>.IndexKeys
                        .Descending("Claims.ClaimValue")
                        .Descending("Claims.ClaimType"),
                    new CreateIndexOptions<TUser>
                    {
                        Background = true
                    }),
                new CreateIndexModel<TUser>(
                    Builders<TUser>.IndexKeys
                        .Descending(v => v.Roles),
                    new CreateIndexOptions<TUser>
                    {
                        Background = true
                    })
            });
        }
    }
}
