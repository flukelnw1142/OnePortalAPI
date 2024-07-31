using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        public UserService(AppDbContext context)
        {
            this._context = context;
        }

        public async Task<bool> DeleteUserById(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _context.User.Where(e => e.user_id == userId).FirstOrDefaultAsync();
            if (user == null)
            {
                return false;
            }

            _context.User.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        public ValueTask<EntityEntry<User>> AddUser(User user, CancellationToken cancellationToken = default)
        {
            return _context.User.AddAsync(user, cancellationToken);
        }

        public Task<User> GetUserById(int user_id, CancellationToken cancellationToken = default)
        {
            return _context.User.Where(e => e.user_id == user_id).FirstOrDefaultAsync();
        }

        public Task<List<User>> GetUsersList(CancellationToken cancellationToken = default)
        {
            return _context.User.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<User>> SearchUsers(string? firstname, string? username, CancellationToken cancellationToken = default)
        {
            var query = _context.User.AsQueryable();

            if (!string.IsNullOrEmpty(firstname))
            {
                query = query.Where(u => u.firstname.Contains(firstname));
            }

            if (!string.IsNullOrEmpty(username))
            {
                query = query.Where(u => u.username.Contains(username));
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<User> UpdateUser(int user_id, UserDto userDto, CancellationToken cancellationToken = default)
        {
            var user = await _context.User.FindAsync(user_id, cancellationToken);

            if (user == null)
            {
                return null;
            }
            //user.user_id = userDto.user_id;
            user.firstname = userDto.firstname;
            user.lastname = userDto.lastname;
            user.username = userDto.username;
            user.password = userDto.password;
            user.email = userDto.email;
            user.Role = userDto.Role;
            user.status = userDto.status;
            user.create_date = userDto.create_date;
            user.update_date = userDto.update_date;
            user.company = userDto.company;
            _context.User.Update(user);
            await _context.SaveChangesAsync(cancellationToken);

            return user;
        }

        public async Task<List<User>> FindApproversByCompany(string company, CancellationToken cancellationToken = default)
        {

            var companyList = company.Split(',').ToList();
            var approvers = await _context.User
                .Where(c => companyList.Any(cl => c.company.Contains(cl)) && c.Role == 3)
                .ToListAsync(cancellationToken);

            return approvers;
        }

        public async Task<List<User>> FindApproversFNByCompany(string company, CancellationToken cancellationToken = default)
        {
            var companyList = company.Split(',').ToList();
            var approvers = await _context.User
                .Where(c => companyList.Any(cl => c.company.Contains(cl)) && c.Role == 4)
                .ToListAsync(cancellationToken);

            return approvers;
        }
    }
}
