using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace OnePortal_Api.Helpers
{
    public class DatabaseHelper(IConfiguration configuration)
    {
        private readonly IConfiguration _configuration = configuration;

        public async Task<List<string>> GetCodesByPrefix(string prefix)
        {
            var codeList = new List<string>();
            using (var sqlConn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await sqlConn.OpenAsync();
                using var cmd = new SqlCommand("GetTempNumKeyByPrefix", sqlConn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@prefix", prefix);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var code = reader["Code"]?.ToString();
                    if (!string.IsNullOrEmpty(code)) codeList.Add(code);
                }
            }
            return codeList;
        }

        public async Task<Dictionary<string, string>> GetMaxNumsFromOracle(string storedProcedure, string keys)
        {
            var results = new Dictionary<string, string>();
            using (var oracleConn = new OracleConnection(_configuration.GetConnectionString("OracleConnection")))
            {
                await oracleConn.OpenAsync();
                using var oracleCmd = new OracleCommand(storedProcedure, oracleConn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                oracleCmd.Parameters.Add("keys", OracleDbType.Varchar2).Value = keys;
                oracleCmd.Parameters.Add("result_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                using var oracleReader = await oracleCmd.ExecuteReaderAsync();

                string? keyColumn = oracleReader.HasColumn("KEY_CUSTOMER") ? "KEY_CUSTOMER" :
                                   oracleReader.HasColumn("KEY") ? "KEY" : null;

                if (keyColumn == null || !oracleReader.HasColumn("MAXNUM"))
                {
                    throw new Exception("Expected columns 'KEY'/'KEY_CUSTOMER' and 'MAXNUM' not found in the result set.");
                }

                var columns = Enumerable.Range(0, oracleReader.FieldCount)
                                         .Select(oracleReader.GetName)
                                         .ToArray();
                Console.WriteLine("Columns in result set: " + string.Join(", ", columns));

                while (await oracleReader.ReadAsync())
                {
                    var key = oracleReader[keyColumn]?.ToString() ?? string.Empty;
                    var maxNum = oracleReader["MAXNUM"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(maxNum))
                    {
                        results[key] = maxNum;
                    }
                }
            }
            return results;
        }

        public async Task UpdateTempNumKeysInSqlServer(Dictionary<string, string> results)
        {
            using var sqlConn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await sqlConn.OpenAsync();

            foreach (var (code, maxNum) in results)
            {
                using var updateCmd = new SqlCommand("UpdateTempNumKey", sqlConn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                updateCmd.Parameters.AddWithValue("@Code", code);
                updateCmd.Parameters.AddWithValue("@MaxNum", maxNum);
                await updateCmd.ExecuteNonQueryAsync();

                using var logCmd = new SqlCommand("InsertUpdateLog", sqlConn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                logCmd.Parameters.AddWithValue("@Code", code);
                logCmd.Parameters.AddWithValue("@MaxNum", maxNum);
                logCmd.Parameters.AddWithValue("@UpdatedBy", "System");
                await logCmd.ExecuteNonQueryAsync();
            }
        }
    }
}