using Microsoft.AspNetCore.Mvc;
using OnePortal_Api.Data;
using OnePortal_Api.Model;
using OnePortal_Api.Services;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : Controller
    {
        private readonly IRoleService _roleServices;
        private readonly AppDbContext _appDbContext;

        public RoleController(IRoleService roleServices, AppDbContext appDbContext)
        {
            _roleServices = roleServices;
            _appDbContext = appDbContext;
        }

        [HttpGet("searchRole")]
        public async Task<IActionResult> SearchRoles([FromQuery] string roleName, CancellationToken cancellationToken)
        {
            var roles = await _roleServices.SearchRoles(roleName, cancellationToken);
            return Ok(roles);
        }
        [HttpPost("AddRole")]
        public async Task<ActionResult<Role>> AddRole(Role role)
        {
            _roleServices.AddRole(role);
            await _appDbContext.SaveChangesAsync();
            return Ok();

        }

        [HttpGet("RoleList")]
        public async Task<ActionResult<IEnumerable<Role>>> GetRoleInfo()
        {
            return await _roleServices.GetRoleList();
        }

        [HttpDelete("DeleteBy/{id}")]
        public async Task<IActionResult> DeleteRoleId(int id, CancellationToken cancellationToken)
        {
            var success = await _roleServices.DeleteRoleID(id, cancellationToken);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
