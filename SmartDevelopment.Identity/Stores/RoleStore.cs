using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using SmartDevelopment.Dal.Abstractions;
using SmartDevelopment.Dal.Abstractions.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace SmartDevelopment.Identity.Stores
{
    public class RoleStore<TRole, TUserRole, TRoleClaim> : RoleStoreBase<TRole, ObjectId, TUserRole, TRoleClaim>
        where TRole : Entities.IdentityRole
        where TUserRole : IdentityUserRole<ObjectId>, new()
        where TRoleClaim : IdentityRoleClaim<ObjectId>, new()
    {
        private readonly IDal<TRole> _dal;

        public override IQueryable<TRole> Roles => _dal.AsQueryable();

        public RoleStore(IDal<TRole> dal, IdentityErrorDescriber describer)
            : base(describer)
        {
            _dal = dal;
        }

        public override async Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken)
        {
            await _dal.InsertAsync(role).ConfigureAwait(false);
            return IdentityResult.Success;
        }

        public override async Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken)
        {
            await _dal.UpdateAsync(role.Id, role).ConfigureAwait(false);
            return IdentityResult.Success;
        }

        public override async Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken)
        {
            await _dal.DeleteAsync(role.Id).ConfigureAwait(false);
            return IdentityResult.Success;
        }

        public override Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Id.ToString());
        }

        public override Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Name);
        }

        public override Task SetRoleNameAsync(TRole role, string roleName, CancellationToken cancellationToken)
        {
            role.Name = roleName;
            return Task.CompletedTask;
        }

        public override Task<string> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.NormalizedName);
        }

        public override Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, CancellationToken cancellationToken)
        {
            role.NormalizedName = normalizedName;
            return Task.CompletedTask;
        }

        public override Task<TRole> FindByIdAsync(string id, CancellationToken cancellationToken)
        {
            return _dal.GetAsync(ConvertIdFromString(id));
        }

        public override async Task<TRole> FindByNameAsync(string normalizedName, CancellationToken cancellationToken)
        {
            var roles = await _dal.GetAsync(PagingInfo.OneItem, v => v.NormalizedName == normalizedName).ConfigureAwait(false);
            return roles.FirstOrDefault();
        }

        public override Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult((IList<Claim>)role.Claims.Select(c => new Claim(c.ClaimType, c.ClaimValue)).ToList());
        }

        public override Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            role.Claims.Add(CreateRoleClaim(role, claim));
            return Task.CompletedTask;
        }

        public override Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            role.Claims.RemoveAll(v => v.ClaimType == claim.Type && v.ClaimValue == claim.Value);
            return Task.CompletedTask;
        }

        public override ObjectId ConvertIdFromString(string id)
        {
            if (ObjectId.TryParse(id, out ObjectId oId))
                return oId;

            return base.ConvertIdFromString(id);
        }
    }
}