using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public class SupplierBankService : ISupplierBankService
    {
        private readonly AppDbContext _context;
        public SupplierBankService(AppDbContext context)
        {
            this._context = context;
        }
        public ValueTask<EntityEntry<SupplierBank>> AddSupplierBank(SupplierBank SupplierBank, CancellationToken cancellationToken = default)
        {
            return _context.supplierBanks.AddAsync(SupplierBank, cancellationToken);
        }

        public Task<SupplierBank> GetSupplierBankByID(int Id, CancellationToken cancellationToken = default)
        {
            return _context.supplierBanks.Where(e => e.supbank_id == Id).FirstOrDefaultAsync();
        }

        public Task<List<SupplierBank>> GetSupplierBankBySupplierId(int supplier, CancellationToken cancellationToken = default)
        {
            return _context.supplierBanks
           .Where(c => c.supplier_id == supplier)
           .ToListAsync(cancellationToken);
        }

        public Task<List<SupplierBank>> GetSupplierBankList(CancellationToken cancellationToken = default)
        {
            return _context.supplierBanks.ToListAsync(cancellationToken);
        }

        public async Task<SupplierBank> UpdateSupplierBank(int id, SupplierBankDto supplierBankDto, CancellationToken cancellationToken = default)
        {
            var supplierBank = await _context.supplierBanks.FirstOrDefaultAsync(s => s.supbank_id == id, cancellationToken);

            if (supplierBank == null)
            {
                return null;
            }

            //supplierBank.supbank_id = supplierBankDto.supbank_id;
            //supplierBank.supplier_id = supplierBankDto.supplier_id;
            supplierBank.name_bank = supplierBankDto.name_bank;
            supplierBank.branch = supplierBankDto.branch;
            supplierBank.account_num = supplierBankDto.account_num;
            supplierBank.account_name = supplierBankDto.account_name;
            supplierBank.supplier_group = supplierBankDto.supplier_group;
            supplierBank.company = supplierBankDto.company;

            _context.supplierBanks.Update(supplierBank);
            await _context.SaveChangesAsync(cancellationToken);

            return supplierBank;
        }
    }
}
