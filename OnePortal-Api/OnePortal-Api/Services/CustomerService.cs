using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly AppDbContext _context;
        public CustomerService(AppDbContext context)
        {
            this._context = context;
        }

        public ValueTask<EntityEntry<Customer>> AddCustomer(Customer customer, CancellationToken cancellationToken = default)
        {
            return _context.Customer.AddAsync(customer, cancellationToken);
        }

        public Task<Customer> GetCustomerByID(int Id, CancellationToken cancellationToken = default)
        {
            return _context.Customer.Where(e => e.Id == Id).FirstOrDefaultAsync();
        }

        public Task<Customer> GetCustomerByTypeName(string customerType, CancellationToken cancellationToken = default)
        {
            return _context.Customer
            .Where(c => c.customer_type == customerType)
            .OrderByDescending(c => c.customer_num)
            .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<List<Customer>> GetCustomerList(CancellationToken cancellationToken = default)
        {
            return _context.Customer.ToListAsync(cancellationToken);
        }

        public Task<Customer> GetDataByTaxID(string taxId, CancellationToken cancellationToken = default)
        {
            return _context.Customer
            .Where(c => c.Tax_Id == taxId)
            .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<Customer>> GetDataByUserCompanyACC(string company, CancellationToken cancellationToken = default)
        {
            var companyList = company.Split(',').ToList();
            var customers = await _context.Customer
                .Where(c => companyList.Any(cl => c.company.Contains(cl)) &&
                            !new[] { "Draft", "Cancel" }.Contains(c.status))
                .ToListAsync(cancellationToken);
            return customers;
        }

        public async Task<List<Customer>> GetDataByUserCompanyFN(string company, CancellationToken cancellationToken = default)
        {
            var companyList = company.Split(',').ToList();
            var customers = await _context.Customer
                .Where(c => companyList.Any(cl => c.company.Contains(cl)) &&
                            !new[] { "Draft", "Cancel" }.Contains(c.status))
                .ToListAsync(cancellationToken);
            return customers;
        }

        public Task<List<Customer>> GetDataByUserId(int userid, CancellationToken cancellationToken = default)
        {
            return _context.Customer
                    .Where(c => c.user_id == userid)
                    .OrderByDescending(c => c.Id)
                    .ToListAsync(cancellationToken);
        }

        public async Task<Customer> UpdateCustomer(int id, CustomerDto customerDto, CancellationToken cancellationToken = default)
        {
            var customer = await _context.Customer.FindAsync(id, cancellationToken);

            if (customer == null)
            {
                return null;
            }

            customer.Name = customerDto.Name;
            customer.Tax_Id = customerDto.Tax_Id;
            customer.address_sup = customerDto.address_sup;
            customer.district = customerDto.district;
            customer.subdistrict = customerDto.subdistrict;
            customer.province = customerDto.province;
            customer.postalCode = customerDto.postalCode;
            customer.tel = customerDto.tel;
            customer.email = customerDto.email;
            customer.customer_num = customerDto.customer_num;
            customer.customer_type = customerDto.customer_type;
            customer.site = customerDto.site;
            customer.status = customerDto.status;
            

            _context.Customer.Update(customer);
            await _context.SaveChangesAsync(cancellationToken);

            return customer;
        }
    }
}
