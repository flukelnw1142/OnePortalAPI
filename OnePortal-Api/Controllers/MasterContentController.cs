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
    public class MasterContentController(AppDbContext appDbContext) : Controller
    {
        private readonly AppDbContext _appDbContext = appDbContext;

        [HttpGet("GetContentById")]
        public async Task<IActionResult> GetContentById(int id)
        {
            try
            {
                var connectionString = _appDbContext.Database.GetConnectionString();

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using (var content = new SqlCommand("GetContentById", connection))
                {
                    content.CommandType = CommandType.StoredProcedure;
                    content.Parameters.AddWithValue("@ID", id);

                    using (var reader = await content.ExecuteReaderAsync())
                    {
                        var result = new List<MasterContent>();

                        while (await reader.ReadAsync())
                        {
                            result.Add(new MasterContent
                            {
                                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                                Title = reader.GetString(reader.GetOrdinal("Title")),
                                Content = reader.GetString(reader.GetOrdinal("Content")),
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

        [HttpGet("GetAnnouncementByUsername")]
        public async Task<IActionResult> GetAnnouncementByUsername(string username)
        {
            try
            {
                var connectionString = _appDbContext.Database.GetConnectionString();

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using (var pdpa = new SqlCommand("GetAnnouncementByUserId", connection))
                {
                    pdpa.CommandType = CommandType.StoredProcedure;
                    pdpa.Parameters.AddWithValue("@Username", username);

                    using (var reader = await pdpa.ExecuteReaderAsync())
                    {
                        var result = new List<Announcement_Consent>();

                        while (await reader.ReadAsync())
                        {
                            result.Add(new Announcement_Consent
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

        [HttpPost("InsertAnnouncement")]
        public async Task<ActionResult> InsertLog(string username)
        {
            var connectionString = _appDbContext.Database.GetConnectionString();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using (var pdpa = new SqlCommand("InsertAnnouncementConsent", connection))
            {
                pdpa.CommandType = CommandType.StoredProcedure;
                pdpa.Parameters.AddWithValue("@Username", username);
                using (var reader = await pdpa.ExecuteReaderAsync())
                {
                    var result = new List<Announcement_Consent>();

                    while (await reader.ReadAsync())
                    {
                        result.Add(new Announcement_Consent
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
    }
}
