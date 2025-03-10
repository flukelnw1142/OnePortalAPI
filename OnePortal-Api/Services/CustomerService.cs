using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;
using System.Threading.Tasks;

namespace OnePortal_Api.Services
{
    public class CustomerService(AppDbContext context) : ICustomerService
    {
        private readonly AppDbContext _context = context;

        public ValueTask<EntityEntry<Customer>> AddCustomer(Customer customer, CancellationToken cancellationToken = default)
        {
            return _context.Customer.AddAsync(customer, cancellationToken);
        }

        public Task<Customer?> GetCustomerByID(int Id, CancellationToken cancellationToken = default)
        {
            return _context.Customer.Where(e => e.Id == Id).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<Customer?>> GetCustomerList(CancellationToken cancellationToken = default)
        {
            var customers = await _context.Customer.ToListAsync(cancellationToken);
            return customers.Cast<Customer?>().ToList();
        }

        public async Task<List<Customer?>> GetDataByTaxID(string taxId, CancellationToken cancellationToken = default)
        {
            var customers = await _context.Customer
                .Where(c => c.TaxId == taxId)
                .OrderByDescending(c => c.Id)
                .ToListAsync(cancellationToken);

            return customers.Cast<Customer?>().ToList();
        }

        private static readonly string[] StatusArray = ["Draft", "Cancel"];

        public async Task<List<Customer>> GetDataByUserCompanyACC(string company, int userId, CancellationToken cancellationToken = default)
        {
            var user = await _context.User
            .Where(u => u.UserId == userId)
            .Select(u => u.ResponseType)
            .FirstOrDefaultAsync(cancellationToken);

            // ถ้า ResponseType ไม่ใช่ 1 หรือ 3 ให้คืนค่ากลับเป็นรายการว่าง
            if (user != 1 && user != 3)
            {
                return new List<Customer>();
            }
            else {
                var companyList = company.Split(',').ToList();

                var customers = await _context.Customer
                    .Where(c => companyList.Any(cl => c.Company.Contains(cl)) &&
                                !StatusArray.Contains(c.Status)).OrderByDescending(c => c.Id)
                    .ToListAsync(cancellationToken);
                return customers;
            }
            
        }

        public async Task<List<Customer?>> GetDataByUserCompanyFN(string company, CancellationToken cancellationToken = default)
        {
            List<string> companyList = [.. company.Split(',')];

            var customers = await _context.Customer
                .Where(c => companyList.Any(cl => c.Company.Contains(cl)) &&
                            c.Status == "Approved By ACC")
                .ToListAsync(cancellationToken);

            return customers.Cast<Customer?>().ToList();
        }

        public async Task<List<Customer>> GetDataByUserId(int userId, CancellationToken cancellationToken = default)
        {
            var parameter = new SqlParameter("@UserId", userId);
            var customers = await _context.Customer
                .FromSqlRaw("EXEC GetCustomerByUserId @UserId", parameter)
                .ToListAsync(cancellationToken);

            return customers;
        }

        public async Task<Customer?> UpdateCustomer(int id, CustomerDto customerDto, CancellationToken cancellationToken = default)
        {
            var customer = await _context.Customer.FindAsync([id], cancellationToken);

            if (customer == null)
            {
                return null;
            }

            customer.Name = customerDto.Name ?? customer.Name;
            customer.TaxId = customerDto.TaxId ?? customer.TaxId;
            customer.AddressSup = customerDto.AddressSup ?? customer.AddressSup;
            customer.District = customerDto.District ?? customer.District;
            customer.Subdistrict = customerDto.Subdistrict ?? customer.Subdistrict;
            customer.Province = customerDto.Province ?? customer.Province;
            customer.PostalCode = customerDto.PostalCode ?? customer.PostalCode;
            customer.Tel = customerDto.Tel ?? customer.Tel;
            customer.Email = customerDto.Email ?? customer.Email;
            customer.CustomerNum = customerDto.CustomerNum ?? customer.CustomerNum;
            customer.CustomerType = customerDto.CustomerType ?? customer.CustomerType;
            customer.Site = customerDto.Site ?? customer.Site;
            customer.Status = customerDto.Status ?? customer.Status;
            customer.Path = customerDto.Path ?? customer.Path;
            customer.FileReq = customerDto.FileReq ?? customer.FileReq;
            customer.FileCertificate = customerDto.FileCertificate ?? customer.FileCertificate;
            customer.PostId = customerDto.PostId ?? customer.PostId;
            customer.AddressDetail = customerDto.AddressDetail ?? customer.AddressDetail;
            _context.Customer.Update(customer);
            await _context.SaveChangesAsync(cancellationToken);

            return customer;
        }

        public async Task<List<CustomerSupplierDto?>> GetCustomerSupplierHistory(int userId, string? company, string? status, string? ownerType, CancellationToken cancellationToken = default)
        {
            var parameters = new[]
            {
                new SqlParameter("@UserId", userId == 0 ? (object)DBNull.Value : userId),
                new SqlParameter("@CompanyList", string.IsNullOrEmpty(company) ? (object)DBNull.Value : company),
                new SqlParameter("@Status", string.IsNullOrEmpty(status) ? (object)DBNull.Value : status),
                new SqlParameter("@OwnerType", string.IsNullOrEmpty(ownerType) ? (object)DBNull.Value : ownerType)
            };

            var result = await _context
                .CustomerSupplierDto
                .FromSqlRaw("EXEC [dbo].[GetCustomerSupplierHistory] @UserId, @CompanyList, @Status, @OwnerType", parameters)
                .ToListAsync(cancellationToken);

            return result.Cast<CustomerSupplierDto?>().ToList();
        }

        public async Task<List<CustomerSupplierDto?>> GetDataHistoryByUserId(int userId, string? company)
        {
            var companyList = company?.Split(',').Select(c => c.Trim()).ToList();
            var query = @"
            SELECT X.*
            FROM (
                SELECT 
                    id,
                    name,
                    taxId,
                    addressSup,
                    district,
                    subdistrict,
                    province,
                    postalCode,
                    tel,
                    email,
                    status,
                    customerNum AS num,
                    customerType AS type,
                    site,
                    '' AS paymentMethod,
                    'Customer' AS source,
                    userId,
                    company,
                    ownerAcc,
                    '' as ownerFn
                FROM [PotalInterFace].[dbo].[customer_info]

                UNION ALL

                SELECT 
                    id,
                    name,
                    tax_Id,
                    addressSup,
                    district,
                    subdistrict,
                    province,
                    postalCode,
                    tel,
                    email,
                    status,
                    supplierNum AS num,
                    supplierType AS type,
                    site,
                    Coalesce(paymentMethod,''), 
                    'Supplier' AS source,
                    userId,
                    company,
                    ownerAcc,
                    ownerFn
                FROM [PotalInterFace].[dbo].[Supplier_info]
            ) X
            WHERE (X.user_id = ISNULL(@UserId, X.userId) AND (@CompanyList IS NULL OR X.company IN (SELECT value FROM STRING_SPLIT(@CompanyList, ','))))";

            var parameters = new[]
            {
        new SqlParameter("@UserId", userId == 0 ? (object)DBNull.Value : userId),
        new SqlParameter("@CompanyList", string.IsNullOrEmpty(company) ? (object)DBNull.Value : company)
    };

            var result = await _context
                .CustomerSupplierDto
                .FromSqlRaw(query, parameters)
                .ToListAsync();

            return result.Cast<CustomerSupplierDto?>().ToList();
        }

        public async Task<List<CustomerSupplierDto?>> GetDataHistoryByApprover(int userId, string? company, string status)
        {
            var query = @"
            SELECT X.*
            FROM (
                SELECT 
                    id,
                    name,
                    TaxId,
                    AddressSup,
                    district,
                    subdistrict,
                    province,
                    postalCode,
                    tel,
                    email,
                    status,
                    CustomerNum AS num,
                    CustomerType AS type,
                    site,
                    '' AS paymentMethod,
                    'Customer' AS source,
                    userId,
                    company,
                    ownerAcc,
                    '' AS owner_fn
                FROM [PotalInterFace].[dbo].[customer_info]

                UNION ALL

                SELECT 
                    id,
                    name,
                    tax_Id,
                    addressSup,
                    district,
                    subdistrict,
                    province,
                    postalCode,
                    tel,
                    email,
                    status,
                    supplierNum AS num,
                    supplierType AS type,
                    site,
                    COALESCE(paymentMethod, ''),
                    'Supplier' AS source,
                    userId,
                    company,
                    ownerAcc,
                    ownerFn
                FROM [PotalInterFace].[dbo].[Supplier_info]
            ) X
            WHERE 
                (X.status = @Status)
                OR (X.owner_acc = @UserId)
                AND X.company IN (SELECT value FROM STRING_SPLIT(@CompanyList, ','))";

            var parameters = new[]
            {
                new SqlParameter("@UserId", userId == 0 ? (object)DBNull.Value : userId),
                new SqlParameter("@CompanyList", string.IsNullOrEmpty(company) ? (object)DBNull.Value : company),
                new SqlParameter("@Status", string.IsNullOrEmpty(status) ? (object)DBNull.Value : status)
            };

            var result = await _context
                .CustomerSupplierDto
                .FromSqlRaw(query, parameters)
                .ToListAsync();

            return result.Cast<CustomerSupplierDto?>().ToList();
        }

        public async Task<List<CustomerSupplierDto?>> GetDataHistoryByApproverFN(int userId, string? company, string status)
        {
            var query = @"
            SELECT X.*
            FROM (
                SELECT 
                    id,
                    name,
                    tax_Id,
                    address_sup,
                    district,
                    subdistrict,
                    province,
                    postalCode,
                    tel,
                    email,
                    status,
                    customer_num AS num,
                    customer_type AS type,
                    site,
                    '' AS payment_method,
                    'Customer' AS source,
                    user_id,
                    company,
                    owner_acc,
                    '' AS owner_fn
                FROM [PotalInterFace].[dbo].[customer_info]

                UNION ALL

                SELECT 
                    id,
                    name,
                    tax_Id,
                    address_sup,
                    district,
                    subdistrict,
                    province,
                    postalCode,
                    tel,
                    email,
                    status,
                    supplier_num AS num,
                    supplier_type AS type,
                    site,
                    COALESCE(payment_method, ''),
                    'Supplier' AS source,
                    user_id,
                    company,
                    owner_acc,
                    owner_fn
                FROM [PotalInterFace].[dbo].[Supplier_info]
            ) X
            WHERE 
                (X.status = @Status)
                OR (X.owner_fn = @UserId)
                AND X.company IN (SELECT value FROM STRING_SPLIT(@CompanyList, ','))";

            var parameters = new[]
            {
                new SqlParameter("@UserId", userId == 0 ? (object)DBNull.Value : userId),
                new SqlParameter("@CompanyList", string.IsNullOrEmpty(company) ? (object)DBNull.Value : company),
                new SqlParameter("@Status", string.IsNullOrEmpty(status) ? (object)DBNull.Value : status)
            };

            var result = await _context
                .CustomerSupplierDto
                .FromSqlRaw(query, parameters)
                .ToListAsync();

            return result.Cast<CustomerSupplierDto?>().ToList();
        }
    }
}