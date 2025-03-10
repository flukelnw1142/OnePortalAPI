using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public class SupplierService(AppDbContext context) : ISupplierService
    {
        private readonly AppDbContext _context = context;

        private static readonly string[] StatusArray = ["Draft", "Cancel"];

        public ValueTask<EntityEntry<Supplier>> AddSupplier(Supplier supplier, CancellationToken cancellationToken = default)
        {
            return _context.Supplier.AddAsync(supplier, cancellationToken);
        }

        public Task<List<Company>> GetCompanyList(CancellationToken cancellationToken = default)
        {
            return _context.Company.ToListAsync(cancellationToken);
        }

        public Task<List<Supplier>> GetDataByTaxID(string taxId, CancellationToken cancellationToken = default)
        {
            return _context.Supplier
                .Where(c => c.Tax_Id == taxId)
                .OrderByDescending(c => c.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Supplier>> GetDataByUserCompanyACC(string company, int userId, CancellationToken cancellationToken = default)
        {
            var user = await _context.User
            .Where(u => u.UserId == userId)
            .Select(u => u.ResponseType)
            .FirstOrDefaultAsync(cancellationToken);

            if (user != 2 && user != 3)
            {
                return new List<Supplier>();
            }
            else {
                var companyList = company.Split(',').ToList();

                var suppliers = await _context.Supplier
                    .Where(c => (c.Company != null && companyList.Any(cl => c.Company.Contains(cl))) &&
                                (c.Status != null && !StatusArray.Contains(c.Status))).OrderByDescending(s => s.Id)
                    .Select(s => new Supplier
                    {
                        Id = s.Id,
                        Prefix = s.Prefix ?? string.Empty,
                        Name = s.Name ?? string.Empty,
                        Tax_Id = s.Tax_Id ?? string.Empty,
                        AddressSup = s.AddressSup ?? string.Empty,
                        District = s.District ?? string.Empty,
                        Subdistrict = s.Subdistrict ?? string.Empty,
                        Province = s.Province ?? string.Empty,
                        PostalCode = s.PostalCode ?? string.Empty,
                        Tel = s.Tel ?? string.Empty,
                        Email = s.Email ?? string.Empty,
                        SupplierNum = s.SupplierNum ?? string.Empty,
                        SupplierType = s.SupplierType ?? string.Empty,
                        Site = s.Site ?? string.Empty,
                        Status = s.Status ?? string.Empty,
                        Vat = s.Vat ?? string.Empty,
                        PaymentMethod = s.PaymentMethod ?? string.Empty,
                        UserId = s.UserId,
                        Company = s.Company ?? string.Empty,
                        Type = s.Type ?? string.Empty,
                        OwnerAcc = s.OwnerAcc.GetValueOrDefault(),
                        OwnerFn = s.OwnerFn.GetValueOrDefault(),
                        Mobile = s.Mobile ?? string.Empty,
                        PostId = s.PostId.GetValueOrDefault()
                    })
                    .ToListAsync(cancellationToken);

                return suppliers;
            }
        }


        public async Task<List<Supplier>> GetDataByUserCompanyFN(string company, CancellationToken cancellationToken = default)
        {
            var companyList = company.Split(',').ToList();

            var suppliers = await _context.Supplier
                .Join(
                    _context.SupplierBanks,
                    supplier => supplier.Id, 
                    bank => bank.SupplierId, 
                    (supplier, bank) => new { Supplier = supplier, Bank = bank }
                )
                .Where(joined => joined.Supplier.Company != null && companyList.Any(cl => joined.Supplier.Company.Contains(cl)) &&
                                 joined.Supplier.Status != null && joined.Supplier.Status == "Approved By ACC" &&
                                 (joined.Supplier.PaymentMethod != null && (joined.Supplier.PaymentMethod == "Transfer" || joined.Supplier.PaymentMethod == "Transfer_Employee")))
                .OrderByDescending(joined => joined.Supplier.Id)
                .Select(joined => new Supplier
                {
                    Id = joined.Supplier.Id,
                    Prefix = joined.Supplier.Prefix ?? string.Empty,
                    Name = joined.Supplier.Name ?? string.Empty,
                    Tax_Id = joined.Supplier.Tax_Id ?? string.Empty,
                    AddressSup = joined.Supplier.AddressSup ?? string.Empty,
                    District = joined.Supplier.District ?? string.Empty,
                    Subdistrict = joined.Supplier.Subdistrict ?? string.Empty,
                    Province = joined.Supplier.Province ?? string.Empty,
                    PostalCode = joined.Supplier.PostalCode ?? string.Empty,
                    Tel = joined.Supplier.Tel ?? string.Empty,
                    Email = joined.Supplier.Email ?? string.Empty,
                    SupplierNum = joined.Supplier.SupplierNum ?? string.Empty,
                    SupplierType = joined.Supplier.SupplierType ?? string.Empty,
                    Site = joined.Supplier.Site ?? string.Empty,
                    Status = joined.Supplier.Status ?? string.Empty,
                    Vat = joined.Supplier.Vat ?? string.Empty,
                    PaymentMethod = joined.Supplier.PaymentMethod ?? string.Empty,
                    UserId = joined.Supplier.UserId,
                    Company = joined.Supplier.Company ?? string.Empty,
                    Type = joined.Supplier.Type ?? string.Empty,
                    OwnerAcc = joined.Supplier.OwnerAcc.GetValueOrDefault(),
                    OwnerFn = joined.Supplier.OwnerFn.GetValueOrDefault(),
                    Mobile = joined.Supplier.Mobile ?? string.Empty,
                    PostId = joined.Supplier.PostId.GetValueOrDefault(),
                    SupplierGroup = joined.Bank.SupplierGroup 
                })
                .ToListAsync(cancellationToken);

            return suppliers;
        }

        public async Task<List<Supplier>> GetDataByUserId(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var parameter = new SqlParameter("@UserId", userId);
                var suppliers = await _context.Supplier
                    .FromSqlRaw("EXEC GetSupplierByUserId @UserId", parameter)
                    .ToListAsync(cancellationToken);

                return suppliers;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDataByUserId: {ex.Message}. StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public Task<List<PaymentMethod>> GetPaymentMethodList(CancellationToken cancellationToken = default)
        {
            return _context.PaymentMethods.ToListAsync(cancellationToken);
        }

        public async Task<Supplier> GetSupplierByID(int Id, CancellationToken cancellationToken = default)
        {
            var supplier = await _context.Supplier.Where(e => e.Id == Id).FirstOrDefaultAsync(cancellationToken);
            return supplier ?? throw new KeyNotFoundException("Supplier not found");
        }

        public async Task<Supplier> GetSupplierByTypeName(string supplier, CancellationToken cancellationToken = default)
        {
            var result = await _context.Supplier
                .Where(c => c.SupplierType == supplier)
                .OrderByDescending(c => c.SupplierNum)
                .FirstOrDefaultAsync(cancellationToken);

            return result ?? throw new KeyNotFoundException("Supplier not found");
        }

        public Task<List<Supplier>> GetSupplierList(CancellationToken cancellationToken = default)
        {
            return _context.Supplier.ToListAsync(cancellationToken);
        }

        public Task<List<Vat>> GetVatList(CancellationToken cancellationToken = default)
        {
            return _context.Vats.ToListAsync(cancellationToken);
        }

        public async Task<Supplier?> UpdateSupplier(int id, SupplierDto supplierDto, CancellationToken cancellationToken = default)
        {
            var supplier = await _context.Supplier.FindAsync([id, cancellationToken], cancellationToken: cancellationToken);

            if (supplier == null)
            {
                return null;
            }

            supplier.Prefix = supplierDto.Prefix;
            supplier.Name = supplierDto.Name;
            supplier.Tax_Id = supplierDto.Tax_Id;
            supplier.AddressSup = supplierDto.AddressSup;
            supplier.District = supplierDto.District;
            supplier.Subdistrict = supplierDto.Subdistrict;
            supplier.Province = supplierDto.Province;
            supplier.PostalCode = supplierDto.PostalCode;
            supplier.Tel = supplierDto.Tel;
            supplier.Email = supplierDto.Email;
            supplier.SupplierNum = supplierDto.SupplierNum;
            supplier.SupplierType = supplierDto.SupplierType;
            supplier.Site = supplierDto.Site;
            supplier.Vat = supplierDto.Vat ?? string.Empty;
            supplier.Status = supplierDto.Status;
            supplier.PaymentMethod = supplierDto.PaymentMethod;
            supplier.Company = supplierDto.Company;
            supplier.Type = supplierDto.Type;
            supplier.OwnerAcc = supplierDto.OwnerAcc;
            supplier.OwnerFn = supplierDto.OwnerFn;
            supplier.Mobile = supplierDto.Mobile;
            _context.Supplier.Update(supplier);
            await _context.SaveChangesAsync(cancellationToken);

            return supplier;
        }
    }
}