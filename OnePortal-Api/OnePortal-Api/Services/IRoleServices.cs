using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public interface IRoleService
    {
        public Task<Role> GetRoleById(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Role>> SearchRoles(string roleName, CancellationToken cancellationToken = default);
        ValueTask<EntityEntry<Role>> AddRole(Role role, CancellationToken cancellationToken = default);
        Task<List<Role>> GetRoleList(CancellationToken cancellationToken = default);
        Task<bool> DeleteRoleID(int RoleId, CancellationToken cancellationToken = default);

    }
}
