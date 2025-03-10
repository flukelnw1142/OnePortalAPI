using OnePortal_Api.Data;
using OnePortal_Api.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace OnePortal_Api.Services
{
    public class CustomerTypeMasterDataService(AppDbContext context) : ICustomerTypeMasterDataService
    {
        private readonly AppDbContext _context = context;

        public async Task<List<CustomerTypeMasterData>> GetCustomerTypeList(CancellationToken cancellationToken = default)
        {
            var customerTypes = await _context.CustomerTypeMasterData
                .FromSqlRaw("EXEC GetAllCustomerTypes")
                .ToListAsync(cancellationToken);

            return customerTypes;
        }

        public async Task<CustomerTypeMasterData?> GetCustomerTypeByID(int id, CancellationToken cancellationToken = default)
        {
            var parameter = new SqlParameter("@Id", id);
            var customerType = _context.CustomerTypeMasterData
                .FromSqlRaw("EXEC GetCustomerTypeByID @Id", parameter)
                .AsEnumerable()
                .FirstOrDefault();

            return await Task.FromResult(customerType);
        }
    }
}