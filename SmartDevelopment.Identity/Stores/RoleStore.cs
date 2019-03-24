using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using SmartDevelopment.Dal.Abstractions;
using SmartDevelopment.Dal.Abstractions.Models;

namespace SmartDevelopment.Identity.Stores
{
    public class RoleStore<TRole> : IRoleStore<TRole>
        where TRole : Entities.IdentityRole
    {
        private readonly IDal<TRole> _dal;

        public RoleStore(IDal<TRole> dal)
        {
            _dal = dal;
        }

        public async Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken)
        {
            await _dal.InsertAsync(role).ConfigureAwait(false);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken)
        {
            await _dal.UpdateAsync(role.Id, role).ConfigureAwait(false);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken)
        {
            await _dal.DeleteAsync(role.Id).ConfigureAwait(false);
            return IdentityResult.Success;
        }

        public Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Id.ToString());
        }

        public Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Name);
        }

        public Task SetRoleNameAsync(TRole role, string roleName, CancellationToken cancellationToken)
        {
            role.Name = roleName;
            return UpdateAsync(role, cancellationToken);
        }

        public Task<string> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.NormalizedName);
        }

        public Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, CancellationToken cancellationToken)
        {
            role.NormalizedName = normalizedName;
            return UpdateAsync(role, cancellationToken);
        }

        public Task<TRole> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            return _dal.GetAsync(ObjectId.Parse(roleId));
        }

        public async Task<TRole> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            return (await _dal.GetAsync(PagingInfo.OneItem, v => v.NormalizedName.Equals(normalizedRoleName)).ConfigureAwait(false)).FirstOrDefault();
        }

        public void Dispose()
        {

        }
    }
}