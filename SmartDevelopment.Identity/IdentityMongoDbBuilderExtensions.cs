using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using SmartDevelopment.Dal.Abstractions;
using SmartDevelopment.Identity.Dals;
using SmartDevelopment.Identity.Stores;
using IdentityRole = SmartDevelopment.Identity.Entities.IdentityRole;
using IdentityUser = SmartDevelopment.Identity.Entities.IdentityUser;

namespace SmartDevelopment.Identity
{
    public static class IdentityMongoDbBuilderExtensions
    {
        public static IdentityBuilder AddMongoDBStores<TUser, TRole>(this IdentityBuilder builder)
            where TUser : IdentityUser
            where TRole : IdentityRole
        {
            return builder.AddMongoDBStores<
                TUser,
                TRole,
                IdentityUserClaim<ObjectId>,
                IdentityUserRole<ObjectId>,
                IdentityUserLogin<ObjectId>,
                IdentityUserToken<ObjectId>,
                IdentityRoleClaim<ObjectId>>();
        }

        public static IdentityBuilder AddMongoDBStores<TUser, TRole, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim>(this IdentityBuilder builder)
            where TUser : IdentityUser
            where TRole : IdentityRole
            where TUserClaim : IdentityUserClaim<ObjectId>, new()
            where TUserRole : IdentityUserRole<ObjectId>, new()
            where TUserLogin : IdentityUserLogin<ObjectId>, new()
            where TUserToken : IdentityUserToken<ObjectId>, new()
            where TRoleClaim : IdentityRoleClaim<ObjectId>, new()
        {
            var services = builder.Services;

            services.AddSingleton<IDal<TUser>, IdentityUserDal<TUser>>();
            services.AddSingleton<IIndexedSource, IdentityUserDal<TUser>>();

            services.AddSingleton<IDal<TRole>, IdentityRoleDal<TRole>>();
            services.AddSingleton<IIndexedSource, IdentityRoleDal<TRole>>();

            return builder
                .AddUserStore<UserStore<TUser, TRole, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim>>()
                .AddRoleStore<RoleStore<TRole, TUserRole, TRoleClaim>>();
        }
    }
}
