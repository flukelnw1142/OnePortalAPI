using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Configuration;

namespace OnePortal_Api.Services
{
    public class OracleDirectService(IConfiguration configuration)
    {
        private readonly string _oracleConnectionString = configuration.GetConnectionString("OracleConnection")
            ?? throw new ArgumentNullException("OracleConnection", "Connection string for Oracle is not configured.");

        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default)
        {
            var result = new List<Dictionary<string, object>>();

            using (var connection = new OracleConnection(_oracleConnectionString))
            {
                await connection.OpenAsync(cancellationToken);

                using var command = new OracleCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var row = new Dictionary<string, object>();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.GetValue(i);
                    }

                    result.Add(row);
                }
            }

            return result;
        }
    }
}