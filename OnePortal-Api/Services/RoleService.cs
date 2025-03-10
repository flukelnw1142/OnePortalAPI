using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Data;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public class RoleService(AppDbContext _context) : IRoleService
    {
        public ValueTask<EntityEntry<Role>> AddRole(Role role, CancellationToken cancellationToken = default)
        {
            return _context.Role.AddAsync(role, cancellationToken);
        }

        public async Task<bool> DeleteRoleID(int RoleId, CancellationToken cancellationToken = default)
        {
            var role = await _context.Role.Where(e => e.Id == RoleId).FirstOrDefaultAsync(cancellationToken: cancellationToken);
            if (role == null)
            {
                return false;
            }

            _context.Role.Remove(role);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<Role> GetRoleById(int id, CancellationToken cancellationToken = default)
        {
            var role = await _context.Role.Where(e => e.Id == id).FirstOrDefaultAsync(cancellationToken);
            return role ?? throw new KeyNotFoundException("Role not found");
        }

        public Task<List<Role>> GetRoleList(CancellationToken cancellationToken = default)
        {
            return _context.Role.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Role>> SearchRoles(string roleName, CancellationToken cancellationToken = default)
        {
            var query = _context.Role.AsQueryable();

            if (!string.IsNullOrEmpty(roleName))
            {
                query = query.Where(r => r.RoleName.Contains(roleName));
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}