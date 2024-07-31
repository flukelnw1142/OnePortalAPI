using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;
using OnePortal_Api.req;
using OnePortal_Api.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase // สืบทอดจาก ControllerBase
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _appDbContext;
        private readonly IRoleService _roleService;

        public UserController(IUserService userService, AppDbContext appDbContext, IRoleService roleServices)
        {
            _userService = userService;
            _appDbContext = appDbContext;
            _roleService = roleServices;
        }

        [HttpGet("UserInfo")]
        public async Task<ActionResult<IEnumerable<User>>> GetUser()
        {
            return await _userService.GetUsersList();
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

        [HttpPost("AddUser")]
        public async Task<ActionResult<User>> AddUser(User user)
        {
            _userService.AddUser(user);
            await _appDbContext.SaveChangesAsync();
            return Ok();

        }


        [HttpGet("findby/{Roleid}")]
        public async Task<ActionResult<Role>> GetRoleById(int Roleid)
        {
            var role = await _roleService.GetRoleById(Roleid);

            if (role == null)
            {
                return NotFound();
            }

            return role;
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
            var users = await _userService.SearchUsers(firstname, username, cancellationToken);
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
        [HttpGet("findApproversFNByCompany")]
        public async Task<IActionResult> findApproversFNByCompany(string company, CancellationToken cancellationToken)
        {
            var approvers = await _userService.FindApproversFNByCompany(company, cancellationToken);
            return Ok(approvers);
        }
    }
}
