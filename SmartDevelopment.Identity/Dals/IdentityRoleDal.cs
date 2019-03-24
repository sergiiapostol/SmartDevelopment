using SmartDevelopment.Dal.MongoDb;
using SmartDevelopment.Identity.Entities;

namespace SmartDevelopment.Identity.Dals
{
    public class IdentityRoleDal : BaseDal<IdentityRole>
    {
        public IdentityRoleDal(IMongoDatabaseFactory databaseFactory) : base(databaseFactory)
        {
        }
    }
}
