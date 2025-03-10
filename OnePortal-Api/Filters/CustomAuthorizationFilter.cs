using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace OnePortal_Api.Filters
{
    public class CustomAuthorizationFilter(IConfiguration configuration) : IAuthorizationFilter
    {
        private readonly string _accessSecret = configuration["JwtSettings:AccessSecret"]
                ?? throw new InvalidOperationException("AccessSecret is not configured.");
        private readonly string _issuer = configuration["JwtSettings:Issuer"]
                ?? throw new InvalidOperationException("Issuer is not configured.");
        private readonly string _audience = configuration["JwtSettings:Audience"]
                ?? throw new InvalidOperationException("Audience is not configured.");

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var token = context.HttpContext.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();

            if (string.IsNullOrEmpty(token))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_accessSecret);

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);

                var nameIdentifier = principal.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

                if (string.IsNullOrEmpty(nameIdentifier) || !nameIdentifier.Contains("Admin"))
                {
                    var routeData = context.RouteData;

                    var controller = routeData.Values["controller"]?.ToString();
                    var action = routeData.Values["action"]?.ToString();

                    if (!string.IsNullOrEmpty(controller) && !string.IsNullOrEmpty(action))
                    {
                        Console.WriteLine($"Unauthorized access to {controller}/{action}");
                    }

                    context.Result = null;
                }
            }
            catch (Exception ex)
            {
                context.Result = new UnauthorizedResult();
                Console.WriteLine($"Authorization error: {ex.Message}");
            }
        }
    }
}