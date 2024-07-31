using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Data;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public class BankMasterDataService : IBankMasterDataService
    {
        private readonly AppDbContext _context;
        public BankMasterDataService(AppDbContext context)
        {
            this._context = context;
        }
        public Task<List<BankMasterData>> GetBankMasterDataList(CancellationToken cancellationToken = default)
        {
            return _context.bankMasters.ToListAsync(cancellationToken);
        }
    }
}
