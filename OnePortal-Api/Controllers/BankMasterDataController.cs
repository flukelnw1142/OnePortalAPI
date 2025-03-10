using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using OnePortal_Api.Data;
using System.Data;
using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Helpers;
using Swashbuckle.AspNetCore.Annotations;
using OnePortal_Api.Filters;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [TypeFilter(typeof(CustomAuthorizationFilter))]
    public class BankMasterDataController(IConfiguration configuration, AppDbContext appDbContext, DatabaseHelper databaseHelper) : Controller
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly AppDbContext _appDbContext = appDbContext;
        private readonly DatabaseHelper _databaseHelper = databaseHelper;

        [HttpGet("BankMasterDataInfo")]
        [SwaggerOperation(Summary = "ดึงข้อมูลแบงค์", Description = "Executes stored procedure to fetch all bank master data from the SQL Server database.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBankMasterDataInfo()
        {
            try
            {
                var bankMasterData = await _appDbContext.BankMasterData.FromSqlRaw("EXEC GetAllBanks").ToListAsync();
                return bankMasterData.Count != 0 ? Ok(bankMasterData) : NotFound("No bank master data found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Query failed: {ex.Message}");
            }
        }

        [HttpGet("KEY_SUPPLIER")]
        [SwaggerOperation(Summary = "คลิกที่ปุ่ม ค้นหาที่ Tab Old Data", Description = "Fetches supplier data based on provided parameters such as num, name, and tax.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Key_Supplier([FromQuery] string? num, string? name, string? tax)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();

                using var command = new OracleCommand("GET_KEY_SUPPLIER", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.Add(new OracleParameter("p_num", OracleDbType.Varchar2, num, ParameterDirection.Input));
                command.Parameters.Add(new OracleParameter("p_name", OracleDbType.Varchar2, name, ParameterDirection.Input));
                command.Parameters.Add(new OracleParameter("p_tax", OracleDbType.Varchar2, tax, ParameterDirection.Input));

                var outputCursor = new OracleParameter("cur", OracleDbType.RefCursor, ParameterDirection.Output);
                command.Parameters.Add(outputCursor);

                using var reader = await command.ExecuteReaderAsync();
                var resultList = new List<Dictionary<string, object>>();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                    }
                    resultList.Add(row);
                }

                return Ok(resultList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Query failed: {ex.Message}");
            }
        }

        [HttpGet("KEY_CUSTOMER")]
        [SwaggerOperation(Summary = "คลิกที่ปุ่ม ค้นหาที่ Tab History", Description = "Fetches customer data based on provided parameters such as num, name, and site.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Key_Customer([FromQuery] string? num, string? name, string? site)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();

                using var command = new OracleCommand("GET_KEY_CUSTOMER", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.Add(new OracleParameter("p_num", OracleDbType.Varchar2, num, ParameterDirection.Input));
                command.Parameters.Add(new OracleParameter("p_name", OracleDbType.Varchar2, name, ParameterDirection.Input));
                command.Parameters.Add(new OracleParameter("p_site", OracleDbType.Varchar2, site, ParameterDirection.Input));

                var outputCursor = new OracleParameter("cur", OracleDbType.RefCursor, ParameterDirection.Output);
                command.Parameters.Add(outputCursor);

                using var reader = await command.ExecuteReaderAsync();
                var resultList = new List<Dictionary<string, object>>();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                    }
                    resultList.Add(row);
                }

                return Ok(resultList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Query failed: {ex.Message}");
            }
        }

        [HttpPost("updateSupplierNumInSqlServer")]
        [SwaggerOperation(Summary = "อัพเดทเลข Supplier ให้ตรงกับ Oracle", Description = "Updates supplier numbers in the SQL Server database based on the latest values retrieved from Oracle.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSupplierNumInSqlServer()
        {
            var codes = await _databaseHelper.GetCodesByPrefix("2");
            if (codes.Count == 0) return NotFound("No supplier codes found with prefix '2'.");

            var results = await _databaseHelper.GetMaxNumsFromOracle("GetMaxNumForKeys", string.Join(",", codes));
            await _databaseHelper.UpdateTempNumKeysInSqlServer(results);

            return Ok("Supplier data updated successfully.");
        }

        [HttpPost("updateCustomerInSqlServer")]
        [SwaggerOperation(Summary = "อัพเดทเลข Customer ให้ตรงกับ Oracle", Description = "Updates customer numbers in the SQL Server database based on the latest values retrieved from Oracle.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCustomerInSqlServer()
        {
            var codes = await _databaseHelper.GetCodesByPrefix("1");
            if (codes.Count == 0) return NotFound("No customer codes found with prefix '1'.");

            var results = await _databaseHelper.GetMaxNumsFromOracle("GetMaxNumForCustomerKeys", string.Join(",", codes));
            await _databaseHelper.UpdateTempNumKeysInSqlServer(results);

            return Ok("Customer data updated successfully.");
        }
    }
}