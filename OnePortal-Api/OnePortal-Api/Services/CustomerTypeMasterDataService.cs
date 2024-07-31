using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Data;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public class CustomerTypeMasterDataService : ICustomerTypeMasterDataService
    {
        private readonly AppDbContext _context;
        public CustomerTypeMasterDataService(AppDbContext context)
        {
            this._context = context;
        }

        public Task<CustomerTypeMasterData> GetCustomerTypeByID(int Id, CancellationToken cancellationToken = default)
        {
            return _context.customerTypeMasters.Where(e => e.id == Id).FirstOrDefaultAsync();
        }

        public Task<List<CustomerTypeMasterData>> GetCustomerTypeList(CancellationToken cancellationToken = default)
        {
            return _context.customerTypeMasters.ToListAsync(cancellationToken);
        }
    }
}
