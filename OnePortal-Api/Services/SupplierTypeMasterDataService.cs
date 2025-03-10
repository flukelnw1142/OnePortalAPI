using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Data;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public class SupplierTypeMasterDataService(AppDbContext _context) : ISupplierTypeMasterDataService
    {
        public async Task<SupplierTypeMasterData?> GetSupplierTypeByID(int id, CancellationToken cancellationToken = default)
        {
            var parameter = new SqlParameter("@Id", id);

            var supplierType = _context.SupplierTypeMasterData
                .FromSqlRaw("EXEC GetSupplierTypeByID @Id", parameter)
                .AsEnumerable()
                .FirstOrDefault();

            return await Task.FromResult(supplierType);
        }

        public Task<List<SupplierTypeMasterData>> GetSupplierTypeList(CancellationToken cancellationToken = default)
        {
            return _context.SupplierTypeMasterData.ToListAsync(cancellationToken);
        }
    }
}