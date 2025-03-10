using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Data;
using OnePortal_Api.Filters;
using OnePortal_Api.Model;
using System.Data;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [TypeFilter(typeof(CustomAuthorizationFilter))]
    public class PDPAConsentController(AppDbContext appDbContext) : Controller
    {
        private readonly AppDbContext _appDbContext = appDbContext;

        [HttpPost("InsertPDPA")]
        public async Task<ActionResult> InsertLog(string username)
        {
            var connectionString = _appDbContext.Database.GetConnectionString();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using (var pdpa = new SqlCommand("InsertPDPAConsent", connection))
            {
                pdpa.CommandType = CommandType.StoredProcedure;
                pdpa.Parameters.AddWithValue("@Username", username);
                using (var reader = await pdpa.ExecuteReaderAsync())
                {
                    var result = new List<PDPAConsent>();

                    while (await reader.ReadAsync())
                    {
                        result.Add(new PDPAConsent
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Username = reader.GetString(reader.GetOrdinal("Username")),
                            Accepted = reader.GetBoolean(reader.GetOrdinal("Accepted")),
                            Time = reader.GetDateTime(reader.GetOrdinal("Time"))
                        });
                    }

                    return Ok(result);
                }
            }
        }

        [HttpGet("GetPDPAByUsername")]
        public async Task<IActionResult> GetPDPA_Accepted(string username)
        {
            try
            {
                var connectionString = _appDbContext.Database.GetConnectionString();

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using (var pdpa = new SqlCommand("GetPDPAByUserId", connection))
                {
                    pdpa.CommandType = CommandType.StoredProcedure;
                    pdpa.Parameters.AddWithValue("@Username", username);

                    using (var reader = await pdpa.ExecuteReaderAsync())
                    {
                        var result = new List<PDPAConsent>();

                        while (await reader.ReadAsync())
                        {
                            result.Add(new PDPAConsent
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Username = reader.GetString(reader.GetOrdinal("Username")),
                                Accepted = reader.GetBoolean(reader.GetOrdinal("Accepted")),
                                Time = reader.GetDateTime(reader.GetOrdinal("Time"))
                            });
                        }

                        return Ok(result);
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Query failed: {ex.Message}");
            }
        }
    }
}
