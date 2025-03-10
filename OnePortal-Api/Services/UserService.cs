using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;
using BCrypt.Net;
using Microsoft.Data.SqlClient;

namespace OnePortal_Api.Services
{
    public class UserService(AppDbContext context) : IUserService
    {
        private readonly AppDbContext _context = context;

        public async Task<bool> DeleteUserById(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _context.User.Where(e => e.UserId == userId).FirstOrDefaultAsync(cancellationToken: cancellationToken);
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

        public async Task AddUserWithEncryptedPassword(User user, CancellationToken cancellationToken = default)
        {
            var sql = "EXEC AddUserWithAutoUpdate @FirstName, @LastName, @Email, @Role, @Status, @UserName, @Password, @Company, @ResponseType, @tel";

            var parameters = new[]
            {
            new SqlParameter("@FirstName", user.Firstname),
            new SqlParameter("@LastName", user.Lastname),
            new SqlParameter("@Email", user.Email),
            new SqlParameter("@Role", user.Role),
            new SqlParameter("@Status", user.Status),
            new SqlParameter("@UserName", user.Username),
            new SqlParameter("@Password", user.Password),
            new SqlParameter("@Company", user.Company),
            new SqlParameter("@ResponseType", user.ResponseType),
            new SqlParameter("@tel", user.tel)
            };

            await _context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
        }


        public Task<User?> GetUserById(int user_id, CancellationToken cancellationToken = default)
        {
            return _context.User.Where(e => e.UserId == user_id).FirstOrDefaultAsync(cancellationToken: cancellationToken);
        }

        public async Task<List<User?>> GetUsersList(CancellationToken cancellationToken = default)
        {
            var users = await _context.User.ToListAsync(cancellationToken);
            return users.Cast<User?>().ToList();
        }

        public async Task<IEnumerable<User?>> SearchUsers(string? firstname, string? username, CancellationToken cancellationToken = default)
        {
            var query = _context.User.AsQueryable();

            if (!string.IsNullOrEmpty(firstname))
            {
                query = query.Where(u => u.Firstname.Contains(firstname));
            }

            if (!string.IsNullOrEmpty(username))
            {
                query = query.Where(u => u.Username.Contains(username));
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<User?> UpdateUser(int user_id, UserDto userDto, CancellationToken cancellationToken = default)
        {
            var user = await _context.User.FindAsync([user_id, cancellationToken], cancellationToken: cancellationToken);

            if (user == null)
            {
                return null;
            }
            //user.user_id = userDto.user_id;
            user.Firstname = userDto.Firstname;
            user.Lastname = userDto.Lastname;
            user.Username = userDto.Username;
            //user.Password = userDto.Password;
            user.Email = userDto.Email;
            user.Role = userDto.Role;
            user.Status = userDto.Status;
            //user.CreateDate = userDto.CreateDate;
            user.UpdateDate = DateTime.Now;
            user.Company = userDto.Company;
            user.ResponseType = userDto.ResponseType;
            user.tel = userDto.tel;
            _context.User.Update(user);
            await _context.SaveChangesAsync(cancellationToken);

            return user;
        }

        public async Task<List<User?>> FindApproversByCompany(string company, CancellationToken cancellationToken = default)
        {

            var companyList = company.Split(',').ToList();
            var approvers = await _context.User
                .Where(c => (companyList.Any(cl => c.Company.Contains(cl)) || c.Company == "ALL") && (c.Role == 3 || c.Role == 1) && (c.ResponseType == 3 || c.ResponseType == 1))
                .ToListAsync(cancellationToken);

            return approvers.Cast<User?>().ToList();
        }
        public async Task<List<User?>> FindApproversByCompanySupplier(string company, CancellationToken cancellationToken = default)
        {

            var companyList = company.Split(',').ToList();
            var approvers = await _context.User
                .Where(c => (companyList.Any(cl => c.Company.Contains(cl)) || c.Company == "ALL") && (c.Role == 3 || c.Role == 1) && (c.ResponseType == 3 || c.ResponseType == 2))
                .ToListAsync(cancellationToken);

            return approvers.Cast<User?>().ToList();
        }

        public async Task<List<User?>> FindApproversFNByCompany(string company, CancellationToken cancellationToken = default)
        {
            var companyList = company.Split(',').ToList();
            var approvers = await _context.User
                .Where(c => (companyList.Any(cl => c.Company.Contains(cl)) || c.Company == "ALL") && (c.Role == 4 || c.Role == 1))
                .ToListAsync(cancellationToken);

            return approvers.Cast<User?>().ToList();
        }

        public async Task<User?> UpdateUserPassword(string username, string newPassword, CancellationToken cancellationToken = default)
        {
            // ค้นหาผู้ใช้โดยใช้ username
            var user = await _context.User.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

            // ตรวจสอบว่าพบผู้ใช้หรือไม่
            if (user == null)
            {
                return null;  // ถ้าไม่พบผู้ใช้ return null
            }

            // ตรวจสอบว่ารหัสผ่านใหม่ไม่เป็นค่าว่าง
            if (string.IsNullOrEmpty(newPassword))
            {
                throw new ArgumentException("New password cannot be empty.");
            }

            // อัปเดตรหัสผ่าน
            user.Password = newPassword;

            // บันทึกการเปลี่ยนแปลง
            _context.User.Update(user);
            await _context.SaveChangesAsync(cancellationToken);

            return user; // ส่งข้อมูลผู้ใช้ที่อัปเดตสำเร็จกลับไป
        }

        public async Task<User?> UpdateDeleteUser(int user_id, int status, CancellationToken cancellationToken = default)
        {
            var user = await _context.User.FindAsync([user_id, cancellationToken], cancellationToken: cancellationToken);

            if (user == null)
            {
                return null;
            }
            //user.user_id = userDto.user_id;
            user.Status = status;

            _context.User.Update(user);
            await _context.SaveChangesAsync(cancellationToken);

            return user;
        }
    }
}
