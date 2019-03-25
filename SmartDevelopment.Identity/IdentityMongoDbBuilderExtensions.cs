using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SmartDevelopment.Dal.Abstractions;
using SmartDevelopment.Identity.Dals;
using SmartDevelopment.Identity.Stores;

namespace SmartDevelopment.Identity
{
    public static class IdentityMongoDbBuilderExtensions
    {
        public static IServiceCollection AddMongoDbIdentity(this IServiceCollection services)
        {
            services.AddSingleton<IDal<Entities.IdentityRole>, IdentityRoleDal>();
            services.AddSingleton<IDal<Entities.IdentityUser>, IdentityUserDal>();
            services.AddSingleton<IIndexedSource, IdentityUserDal>();

            services.AddSingleton(typeof(IRoleStore<>), typeof(UserStore<>));

            services.AddSingleton(typeof(IUserPasswordStore<>), typeof(UserStore<>));
            services.AddSingleton(typeof(IUserRoleStore<>), typeof(UserStore<>));
            services.AddSingleton(typeof(IUserLoginStore<>), typeof(UserStore<>));
            services.AddSingleton(typeof(IUserSecurityStampStore<>), typeof(UserStore<>));
            services.AddSingleton(typeof(IUserEmailStore<>), typeof(UserStore<>));
            services.AddSingleton(typeof(IUserClaimStore<>), typeof(UserStore<>));
            services.AddSingleton(typeof(IUserPhoneNumberStore<>), typeof(UserStore<>));
            services.AddSingleton(typeof(IUserTwoFactorStore<>), typeof(UserStore<>));
            services.AddSingleton(typeof(IUserLockoutStore<>), typeof(UserStore<>));
            services.AddSingleton(typeof(IUserAuthenticationTokenStore<>), typeof(UserStore<>));

            return services;
        }

        public static IdentityBuilder AddMongodbStores(this IdentityBuilder builder)
        {
            return builder
                .AddRoleStore<RoleStore<Entities.IdentityRole>>()
                .AddUserStore<UserStore<Entities.IdentityUser>>();
        }
    }
}
