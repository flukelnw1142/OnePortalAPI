using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Data;
using OnePortal_Api.req;
using OnePortal_Api.Services;


namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : Controller
    {
        private readonly AppDbContext _appDbContext;
        private readonly IUserService _userService;

        public LoginController(AppDbContext appDbContext, IUserService userService)
        {
            _appDbContext = appDbContext;
            _userService = userService;
        }

        [HttpPost("signIn")]
        public async Task<IActionResult> CheckUser(LoginReq loginReq)
        {
            var user = await _appDbContext.User
            .FirstOrDefaultAsync(u => u.username == loginReq.username && u.password == loginReq.password);

            if (user != null)
            {
                return Ok(user);
            }
            else
            {
                return NotFound("User does not exist");
            }
        }
    }
}
