using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Data;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController(AppDbContext appDbContext) : Controller
    {
        private readonly AppDbContext _appDbContext = appDbContext;

        [HttpGet("RoleList")]
        public async Task<IActionResult> GetRoleInfo()
        {
            try
            {
                var roles = await _appDbContext.Role
                    .FromSqlRaw("EXEC GetAllRoles")
                    .ToListAsync();

                if (roles != null && roles.Count != 0)
                {
                    return Ok(roles);
                }
                else
                {
                    return NotFound("No roles found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Query failed: {ex.Message}");
            }
        }
    }
}