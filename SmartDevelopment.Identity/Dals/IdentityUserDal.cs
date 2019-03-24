using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using SmartDevelopment.Dal.Abstractions;
using SmartDevelopment.Dal.MongoDb;
using SmartDevelopment.Identity.Entities;

namespace SmartDevelopment.Identity.Dals
{
    public class IdentityUserDal : BaseDal<IdentityUser>, IIndexedSource
    {
        public IdentityUserDal(IMongoDatabaseFactory databaseFactory) : base(databaseFactory)
        {
        }

        public Task EnsureIndex()
        {
            return Collection.Indexes.CreateManyAsync(new List<CreateIndexModel<IdentityUser>>
            {
                new CreateIndexModel<IdentityUser>(
                    Builders<IdentityUser>.IndexKeys
                        .Descending(v => v.NormalizedUserName),
                    new CreateIndexOptions<IdentityUser>
                    {
                        Unique = true,
                        Background = true
                    }),
                new CreateIndexModel<IdentityUser>(
                    Builders<IdentityUser>.IndexKeys
                        .Descending(v => v.NormalizedEmail),
                    new CreateIndexOptions<IdentityUser>
                    {
                        Background = true
                    }),
                new CreateIndexModel<IdentityUser>(
                    Builders<IdentityUser>.IndexKeys
                        .Descending("Logins.LoginProvider")
                        .Descending("Logins.ProviderKey"),
                    new CreateIndexOptions<IdentityUser>
                    {
                        Background = true
                    })
            });
        }
    }
}
