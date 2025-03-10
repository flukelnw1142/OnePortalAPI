using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using OnePortal_Api.Data;
using OnePortal_Api.Model;
using OnePortal_Api.req;
using System.IdentityModel.Tokens.Jwt;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController(AppDbContext appDbContext, IConfiguration configuration) : Controller
    {
        private readonly AppDbContext _appDbContext = appDbContext;
        private readonly string usernameToken = configuration.GetSection("ConnectionSSO")["usernameToken"] ?? string.Empty;
        private readonly string passwordToken = configuration.GetSection("ConnectionSSO")["passwordToken"] ?? string.Empty;

        [HttpPost("signIn")]
        public async Task<IActionResult> CheckUser(LoginReq loginReq)
        {

            var allowedUsernames = new List<string> { "admin", "IT_ACC", "IT_FIN", "JAN_ADMIN", "user" };

            if (allowedUsernames.Contains(loginReq.Username))
            {
                var parameters = new[]
                {
                 new SqlParameter("@Username", loginReq.Username),
                 new SqlParameter("@Password", loginReq.Password)
            };

                var users = await _appDbContext.User
                    .FromSqlRaw("EXEC [dbo].[GetUserByDecryptedPassword] @Username, @Password", parameters)
                    .ToListAsync();
                
                var user = users.FirstOrDefault(u => u.Username == loginReq.Username);

                if (user != null)
                {
                    var loginResult = await AuthenticateUser(usernameToken, passwordToken);
                    var JwtToken = loginResult?.JwtToken;
                    return Ok(new
                    {
                        User = user,
                        loginResult?.JwtToken
                    });
                }
                else
                {
                    return NotFound("User does not exist");
                }
            }
            else
            {
                var loginResult = await AuthenticateUser(loginReq.Username, loginReq.Password);

                if (loginResult != null)
                {
                    var user = await _appDbContext.User
                        .FirstOrDefaultAsync(u => u.Username == loginResult.SamAccountName);

                    if (user != null)
                    {
                        // ✅ บันทึก Log เมื่อ SSO Login สำเร็จ
                        await _appDbContext.Database.ExecuteSqlRawAsync(
                            "EXEC [dbo].[LogUserLogin] @UserId, @Username, @LoginStatus",
                            new SqlParameter("@UserId", user.UserId),
                            new SqlParameter("@Username", loginReq.Username),
                            new SqlParameter("@LoginStatus", "SSO Success")
                        );

                        return Ok(new
                        {
                            User = user,
                            loginResult?.JwtToken
                        });
                    }
                    else
                    {
                        // ❌ บันทึก Log เมื่อ SSO Login สำเร็จ แต่ไม่พบ User ใน Database
                        await _appDbContext.Database.ExecuteSqlRawAsync(
                            "EXEC [dbo].[LogUserLogin] NULL, @Username, @LoginStatus",
                            new SqlParameter("@Username", loginReq.Username),
                            new SqlParameter("@LoginStatus", "SSO User Not Found")
                        );

                        return NotFound("SSO Login successful but user does not exist in database");
                    }
                }
                else
                {
                    // ❌ บันทึก Log เมื่อ SSO Login ล้มเหลว
                    await _appDbContext.Database.ExecuteSqlRawAsync(
                        "EXEC [dbo].[LogUserLogin] NULL, @Username, @LoginStatus",
                        new SqlParameter("@Username", loginReq.Username),
                        new SqlParameter("@LoginStatus", "SSO Login Failed")
                    );

                    return Unauthorized("SSO Login failed");
                }
            }
        }

        private static async Task<LoginResult?> AuthenticateUser(string username, string password)
        {
            var httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var httpClient = new HttpClient(httpClientHandler);
            try
            {
                var response = await httpClient.PostAsJsonAsync("https://10.10.0.28:7054/api/auth/token", new
                {
                    Username = username,
                    Password = password
                });

                if (response.IsSuccessStatusCode)
                {
                    var tokenData = await response.Content.ReadFromJsonAsync<TokenResponse>();
                    if (tokenData != null)
                    {
                        var jwtHandler = new JwtSecurityTokenHandler();
                        var jwtToken = jwtHandler.ReadJwtToken(tokenData.AccessToken);

                        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

                        if (string.IsNullOrEmpty(subClaim))
                            return null;

                        var samAccountName = subClaim.Split('|').Last();
                        var department = tokenData.Department ?? "Unknown";

                        return new LoginResult
                        {
                            SamAccountName = samAccountName,
                            Department = department,
                            JwtToken = tokenData.AccessToken
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication error: {ex.Message}");
            }

            return null;
        }
    }
    public class LoginResult
    {
        public string SamAccountName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string JwtToken { get; set; } = string.Empty;
    }
    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string? Department { get; set; }
    }
}
