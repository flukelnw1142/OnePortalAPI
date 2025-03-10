using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public interface IUserService
    {
        Task<List<User?>> GetUsersList(CancellationToken cancellationToken = default);
        Task<User?> GetUserById(int user_id,CancellationToken cancellationToken = default);
        ValueTask<EntityEntry<User>> AddUser(User user, CancellationToken cancellationToken = default);
        Task AddUserWithEncryptedPassword(User user, CancellationToken cancellationToken = default); // เพิ่มฟังก์ชันนี้
        Task<bool> DeleteUserById(int userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<User?>> SearchUsers(string firstname, string username, CancellationToken cancellationToken = default);
        Task<User?> UpdateUser(int user_id, UserDto userDto, CancellationToken cancellationToken = default);
        Task<List<User?>> FindApproversByCompany(string company, CancellationToken cancellationToken = default);
        Task<List<User?>> FindApproversByCompanySupplier(string company, CancellationToken cancellationToken = default);
        Task<List<User?>> FindApproversFNByCompany(string company, CancellationToken cancellationToken = default);
        Task<User?> UpdateUserPassword(string username, string newPassword, CancellationToken cancellationToken = default);
        Task<User?> UpdateDeleteUser(int user_id, int status, CancellationToken cancellationToken = default);

    }
}
