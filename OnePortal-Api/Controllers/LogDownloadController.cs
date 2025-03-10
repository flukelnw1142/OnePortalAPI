using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace OnePortal_Api.Controllers
{
    [Route("api/log-download")]
    [ApiController]
    public class LogDownloadController(IConfiguration configuration) : ControllerBase
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("DefaultConnection", "Connection string is missing in appsettings.json");

        [HttpPost]
        public async Task<IActionResult> LogDownload([FromBody] LogDownloadRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.FileUrl))
            {
                return BadRequest("Invalid request data");
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using var command = new SqlCommand("[dbo].[LogDownload]", connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 255) { Value = request.Username });
                    command.Parameters.Add(new SqlParameter("@FileName", SqlDbType.NVarChar, 500) { Value = System.IO.Path.GetFileName(request.FileUrl) });
                    command.Parameters.Add(new SqlParameter("@FileUrl", SqlDbType.NVarChar, 1000) { Value = request.FileUrl });

                    await command.ExecuteNonQueryAsync();
                }

                return Ok(new { message = "Download log recorded" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    public class LogDownloadRequest
    {
        public required string Username { get; set; }
        public required string FileUrl { get; set; }
    }
}