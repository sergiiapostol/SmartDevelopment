using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using SmartDevelopment.Dal.Abstractions;
using SmartDevelopment.Dal.Abstractions.Models;
using IdentityRole = SmartDevelopment.Identity.Entities.IdentityRole;
using IdentityUser = SmartDevelopment.Identity.Entities.IdentityUser;

namespace SmartDevelopment.Identity.Stores
{
    public class UserStore<TUser, TRole, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim> :
        UserStoreBase<TUser, TRole, ObjectId, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim>
        where TUser : IdentityUser
        where TRole : IdentityRole
        where TUserClaim : IdentityUserClaim<ObjectId>, new()
        where TUserRole : IdentityUserRole<ObjectId>, new()
        where TUserLogin : IdentityUserLogin<ObjectId>, new()
        where TUserToken : IdentityUserToken<ObjectId>, new()
        where TRoleClaim : IdentityRoleClaim<ObjectId>, new()

    {
	    private readonly IDal<TUser> _dal;

        private readonly IDal<TRole> _rolesDal;

        public override IQueryable<TUser> Users => _dal.AsQueryable();

        public UserStore(IDal<TUser> dal, IDal<TRole> rolesDal, IdentityErrorDescriber describer) :
            base(describer)
	    {
	        _dal = dal;
            _rolesDal = rolesDal;
        }

        public override async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            await _dal.InsertOrUpdateAsync(new List<TUser> { user }).ConfigureAwait(false);
            return IdentityResult.Success;
        }

        public override async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            await _dal.UpdateAsync(new List<TUser> { user }).ConfigureAwait(false);
            return IdentityResult.Success;
        }

        public override async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            await _dal.DeleteAsync(user).ConfigureAwait(false);
            return IdentityResult.Success;
        }

        public override Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var id = ConvertIdFromString(userId);
            return _dal.GetAsync(id);
        }

        public override async Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var users = await _dal.GetAsync(PagingInfo.OneItem, v => v.NormalizedUserName.Equals(normalizedUserName)).ConfigureAwait(false);
            return users.FirstOrDefault();
        }

        protected override Task<TUser> FindUserAsync(ObjectId userId, CancellationToken cancellationToken)
        {
            return _dal.GetAsync(userId);
        }

        protected override async Task<TUserLogin> FindUserLoginAsync(ObjectId userId, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            var user = await _dal.GetAsync(userId).ConfigureAwait(false);

            return (TUserLogin)user?.Logins.Find(l =>
                    l.LoginProvider == loginProvider
                    && l.ProviderKey == providerKey);
        }

        protected override async Task<TUserLogin> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            var user = await _dal.GetAsync(new PagingInfo(0, 2), v =>
                v.Logins.Any(l =>
                    l.LoginProvider == loginProvider
                    && l.ProviderKey == providerKey)).ConfigureAwait(false);

            return (TUserLogin)user.SingleOrDefault()?.Logins.Find(l =>
                    l.LoginProvider == loginProvider
                    && l.ProviderKey == providerKey);
        }

        public override Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult((IList<Claim>)user.Claims.Select(c => c.ToClaim()).ToList());
        }

        public override Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var claim in claims)
            {
                user.Claims.Add(CreateUserClaim(user, claim));
            }
            return Task.CompletedTask;
        }

        public override Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default(CancellationToken))
        {
            var matchedClaims = user.Claims.Where(uc =>
                uc.ClaimValue == claim.Value
                && uc.ClaimType == claim.Type).ToList();

            foreach (var matchedClaim in matchedClaims)
            {
                matchedClaim.ClaimValue = newClaim.Value;
                matchedClaim.ClaimType = newClaim.Type;
            }
            return Task.CompletedTask;
        }

        public override Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var claim in claims)
            {
                var matchedClaims = user.Claims.Where(uc =>
                    uc.ClaimValue == claim.Value
                    && uc.ClaimType == claim.Type).ToList();

                foreach (var c in matchedClaims)
                {
                    user.Claims.Remove(c);
                }
            }
            return Task.CompletedTask;
        }

        public override Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
        {
            user.Logins.Add(CreateUserLogin(user, login));
            return Task.CompletedTask;
        }

        public override Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            user.Logins.RemoveAll(v=>v.LoginProvider == loginProvider && v.ProviderKey == providerKey);
            return Task.CompletedTask;
        }

        public override Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult((IList<UserLoginInfo>)user.Logins
                .Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName)).ToList());
        }

        public override async Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken))
        {
            var users = await _dal.GetAsync(PagingInfo.OneItem, u => u.NormalizedEmail == normalizedEmail).ConfigureAwait(false);
            return users.FirstOrDefault();
        }

        public override async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _dal.GetAsync(null, v => v.Claims.Any(userclaims => userclaims.ClaimValue == claim.Value
                          && userclaims.ClaimType == claim.Type)).ConfigureAwait(false);
        }

        protected override Task<TUserToken> FindTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            return Task.FromResult((TUserToken)user.Tokens.Find(v=>v.LoginProvider == loginProvider && v.Name == name));
        }

        protected override async Task AddUserTokenAsync(TUserToken token)
        {
            var user = await _dal.GetAsync(token.UserId).ConfigureAwait(false);
            user.Tokens.Add(token);
            await UpdateAsync(user).ConfigureAwait(false);
        }

        protected override async Task RemoveUserTokenAsync(TUserToken token)
        {
            var user = await _dal.GetAsync(token.UserId).ConfigureAwait(false);
            user.Tokens.Remove(token);
            await UpdateAsync(user).ConfigureAwait(false);
        }

        public override async Task<IList<TUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _dal.GetAsync(null, v => v.Roles.Contains(normalizedRoleName)).ConfigureAwait(false);
        }

        public override Task AddToRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!user.Roles.Contains(normalizedRoleName))
                user.Roles.Add(normalizedRoleName);

            return Task.CompletedTask;
        }

        public override Task RemoveFromRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!user.Roles.Contains(normalizedRoleName))
                user.Roles.Remove(normalizedRoleName);

            return Task.CompletedTask;
        }

        public override Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult((IList<string>)user.Roles);
        }

        public override Task<bool> IsInRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(user.Roles.Contains(normalizedRoleName));
        }

        protected override async Task<TRole> FindRoleAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            var roles = await _rolesDal.GetAsync(new PagingInfo(0, 2), v => v.NormalizedName == normalizedRoleName).ConfigureAwait(false);
            return roles.SingleOrDefault();
        }

        protected override async Task<TUserRole> FindUserRoleAsync(ObjectId userId, ObjectId roleId, CancellationToken cancellationToken)
        {
            var role = _rolesDal.GetAsync(userId);
            var user = _dal.GetAsync(userId);

            await Task.WhenAll(role, user).ConfigureAwait(false);

            if (role.Result != null && user.Result != null)
                return new TUserRole { RoleId = roleId, UserId = userId };

            return null;
        }

        public override ObjectId ConvertIdFromString(string id)
        {
            if (ObjectId.TryParse(id, out ObjectId oId))
                return oId;

            return base.ConvertIdFromString(id);
        }
    }
}