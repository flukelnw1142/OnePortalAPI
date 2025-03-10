using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Data;
using OnePortal_Api.Filters;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [TypeFilter(typeof(CustomAuthorizationFilter))]
    public class PrefixController(AppDbContext appDbContext) : Controller
    {
        private readonly AppDbContext _appDbContext = appDbContext;

        [HttpGet("getAllPrefixes")]
        public async Task<IActionResult> GetAllPrefixes()
        {
            var prefixes = await _appDbContext.Prefix
                .FromSqlRaw("EXEC GetAllPrefixes")
                .ToListAsync();

            if (prefixes != null && prefixes.Count != 0)
            {
                return Ok(prefixes);
            }
            else
            {
                return NotFound("No prefixes found.");
            }
        }
    }
}