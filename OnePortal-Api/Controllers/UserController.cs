using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Filters;
using OnePortal_Api.Model;
using OnePortal_Api.req;
using OnePortal_Api.Services;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [TypeFilter(typeof(CustomAuthorizationFilter))]
    public class UserController(IUserService userService, AppDbContext appDbContext) : ControllerBase
    {
        private readonly IUserService _userService = userService;
        private readonly AppDbContext _appDbContext = appDbContext;

        [HttpGet("UserInfo")]
        public async Task<IActionResult> GetUser()
        {
            try
            {
                var users = await _appDbContext.User
                    .FromSqlRaw("EXEC GetAllUsers")
                    .ToListAsync();

                if (users != null && users.Count != 0)
                {
                    return Ok(users);
                }
                else
                {
                    return NotFound("No users found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Query failed: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _userService.GetUserById(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        //[HttpPost("AddUser")]
        //public async Task<ActionResult<User>> AddUser(User user)
        //{
        //    await _userService.AddUser(user);
        //    await _appDbContext.SaveChangesAsync();
        //    return Ok();

        //}

        [HttpPost("AddUser")]
        public async Task<ActionResult> AddUser(User user)
        {
            try
            {
                await _userService.AddUserWithEncryptedPassword(user);  // เรียกใช้ฟังก์ชันที่เข้ารหัสรหัสผ่าน
                return Ok(new { message = "User added successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("findby/{Roleid}")]
        public async Task<ActionResult<Role>> GetRoleById(int Roleid)
        {
            var parameters = new[] { new SqlParameter("@Roleid", Roleid) };

            var roles = await _appDbContext.Role
                .FromSqlRaw("EXEC GetRoleById @Roleid", parameters)
                .ToListAsync();

            var role = roles.FirstOrDefault();

            if (role == null)
            {
                return NotFound();
            }

            return Ok(role);
        }

        [HttpDelete("DeleteBy/{id}")]
        public async Task<IActionResult> DeleteUserById(int id, CancellationToken cancellationToken)
        {
            var success = await _userService.DeleteUserById(id, cancellationToken);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string? firstname, [FromQuery] string? username, CancellationToken cancellationToken)
        {
            var users = await _userService.SearchUsers(firstname ?? string.Empty, username ?? string.Empty, cancellationToken);
            return Ok(users);
        }

        [HttpPut("UpdateUser")]
        public async Task<ActionResult<User>> UpdateUser(int user_id, UserDto userDto)
        {
            var result = await _userService.UpdateUser(user_id, userDto);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpGet("findApproversByCompany")]
        public async Task<IActionResult> FindApproversByCompany(string company, CancellationToken cancellationToken)
        {
            var approvers = await _userService.FindApproversByCompany(company, cancellationToken);
            return Ok(approvers);
        }
        [HttpGet("findApproversByCompanySupplier")]
        public async Task<IActionResult> findApproversByCompanySupplier(string company, CancellationToken cancellationToken)
        {
            var approvers = await _userService.FindApproversByCompanySupplier(company, cancellationToken);
            return Ok(approvers);
        }
        [HttpGet("findApproversFNByCompany")]
        public async Task<IActionResult> FindApproversFNByCompany(string company, CancellationToken cancellationToken)
        {
            var approvers = await _userService.FindApproversFNByCompany(company, cancellationToken);
            return Ok(approvers);
        }

        [HttpPost("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto updatePasswordDto)
        {
            var user = await _userService.UpdateUserPassword(updatePasswordDto.Username, updatePasswordDto.NewPassword);

            if (user != null)
            {
                return Ok(new { message = "Password updated successfully." });
            }
            else
            {
                return NotFound(new { message = "User not found." });
            }
        }

        [HttpGet("find-user/{username}")]
        public async Task<IActionResult> FindUserByUsername(string username)
        {
            var user = await _appDbContext.User.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(user);
        }

        [HttpPut("UpdateUserDelete")]
        public async Task<ActionResult<User>> UpdateUserDelete(int user_id, int status)
        {
            var result = await _userService.UpdateDeleteUser(user_id, status);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpGet("GetAllUserResponsible")]
        public async Task<IActionResult> GetAllUserResponsible()
        {
            try
            {
                var usesRes = await _appDbContext.userResponsibles
                    .FromSqlRaw("EXEC GetAllUserResponsible")
                    .ToListAsync();

                if (usesRes != null && usesRes.Count != 0)
                {
                    return Ok(usesRes);
                }
                else
                {
                    return NotFound("No usesRes found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Query failed: {ex.Message}");
            }
        }

    }
}