using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Data;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public class SupplierTypeMasterDataService : ISupplierTypeMasterDataService
    {
        private readonly AppDbContext _context;
        public SupplierTypeMasterDataService(AppDbContext context)
        {
            this._context = context;
        }

        public Task<SupplierTypeMasterData> GetSupplierTypeByID(int Id, CancellationToken cancellationToken = default)
        {
            return _context.supplierTypeMasters.Where(e => e.id == Id).FirstOrDefaultAsync();
        }

        public Task<List<SupplierTypeMasterData>> GetSupplierTypeList(CancellationToken cancellationToken = default)
        {
            return _context.supplierTypeMasters.ToListAsync(cancellationToken);
        }
    }
}
