using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly AppDbContext _context;
        public SupplierService(AppDbContext context)
        {
            this._context = context;
        }
        public ValueTask<EntityEntry<Supplier>> AddSupplier(Supplier supplier, CancellationToken cancellationToken = default)
        {
            return _context.Supplier.AddAsync(supplier, cancellationToken);
        }

        public Task<List<Company>> GetCompanyList(CancellationToken cancellationToken = default)
        {
            return _context.companies.ToListAsync(cancellationToken);
        }

        public Task<Supplier> GetDataByTaxID(string taxId, CancellationToken cancellationToken = default)
        {
            return _context.Supplier
           .Where(c => c.Tax_Id == taxId)
           .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<Supplier>> GetDataByUserCompanyACC(string company, CancellationToken cancellationToken = default)
        {
            var companyList = company.Split(',').ToList();
            var suppliers = await _context.Supplier
                .Where(c => companyList.Any(cl => c.company.Contains(cl)) &&
                !new[] { "Draft", "Cancel" }.Contains(c.status))
                .ToListAsync(cancellationToken);
            return suppliers;
        }
            public async Task<List<Supplier>> GetDataByUserCompanyFN(string company, CancellationToken cancellationToken = default)
        {
            var companyList = company.Split(',').ToList();
            var suppliers = await _context.Supplier
                .Where(c => companyList.Any(cl => c.company.Contains(cl)) &&
                !new[] { "Draft", "Cancel" }.Contains(c.status))
                .ToListAsync(cancellationToken);
            return suppliers;
        }

        public Task<List<Supplier>> GetDataByUserId(int userid, CancellationToken cancellationToken = default)
        {
            return _context.Supplier
       .Where(c => c.user_id == userid)
       .OrderByDescending(c => c.Id)
       .ToListAsync(cancellationToken);
        }

        public Task<List<PaymentMethod>> GetPaymentMethodList(CancellationToken cancellationToken = default)
        {
            return _context.paymentMethods.ToListAsync(cancellationToken);
        }

        public Task<Supplier> GetSupplierByID(int Id, CancellationToken cancellationToken = default)
        {
            return _context.Supplier.Where(e => e.Id == Id).FirstOrDefaultAsync();
        }

        public Task<Supplier> GetSupplierByTypeName(string supplier, CancellationToken cancellationToken = default)
        {
            return _context.Supplier
            .Where(c => c.supplier_type == supplier)
            .OrderByDescending(c => c.supplier_num)
            .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<List<Supplier>> GetSupplierList(CancellationToken cancellationToken = default)
        {
            return _context.Supplier.ToListAsync(cancellationToken);
        }

        public Task<List<Vat>> GetVatList(CancellationToken cancellationToken = default)
        {
            return _context.vats.ToListAsync(cancellationToken);
        }

        public async Task<Supplier> UpdateSupplier(int id, SupplierDto supplierDto, CancellationToken cancellationToken = default)
        {
            var supplier = await _context.Supplier.FindAsync(id, cancellationToken);

            if (supplier == null)
            {
                return null;
            }

            supplier.Name = supplierDto.Name;
            supplier.Tax_Id = supplierDto.Tax_Id;
            supplier.address_sup = supplierDto.address_sup;
            supplier.district = supplierDto.district;
            supplier.subdistrict = supplierDto.subdistrict;
            supplier.province = supplierDto.province;
            supplier.postalCode = supplierDto.postalCode;
            supplier.tel = supplierDto.tel;
            supplier.email = supplierDto.email;
            supplier.supplier_num = supplierDto.supplier_num;
            supplier.supplier_type = supplierDto.supplier_type;
            supplier.site = supplierDto.site;
            supplier.vat = supplierDto.vat;
            supplier.status = supplierDto.status;
            supplier.payment_method = supplierDto.payment_method;
            supplier.company = supplierDto.company;
            _context.Supplier.Update(supplier);
            await _context.SaveChangesAsync(cancellationToken);

            return supplier;
        }
    }
}
