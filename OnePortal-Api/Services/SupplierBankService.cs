using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public class SupplierBankService(AppDbContext context) : ISupplierBankService
    {
        private readonly AppDbContext _context = context;

        public ValueTask<EntityEntry<SupplierBank>> AddSupplierBank(SupplierBank SupplierBank, CancellationToken cancellationToken = default)
        {
            return _context.SupplierBanks.AddAsync(SupplierBank, cancellationToken);
        }
            
        public async Task<SupplierBank> GetSupplierBankByID(int Id, CancellationToken cancellationToken = default)
        {
            var result = await _context.SupplierBanks.Where(e => e.SupbankId == Id).FirstOrDefaultAsync(cancellationToken: cancellationToken);

            return result ?? throw new KeyNotFoundException("SupplierBank not found");
        }

        public Task<List<SupplierBank>> GetSupplierBankBySupplierId(int supplier, CancellationToken cancellationToken = default)
        {
            return _context.SupplierBanks
           .Where(c => c.SupplierId == supplier)
           .ToListAsync(cancellationToken);
        }

        public Task<List<SupplierBank>> GetSupplierBankList(CancellationToken cancellationToken = default)
        {
            return _context.SupplierBanks.ToListAsync(cancellationToken);
        }

        public async Task<SupplierBank?> UpdateSupplierBank(int id, SupplierBankDto supplierBankDto, CancellationToken cancellationToken = default)
        {
            var supplierBank = await _context.SupplierBanks.FirstOrDefaultAsync(s => s.SupbankId == id, cancellationToken);

            if (supplierBank == null)
            {
                return null;
            }

            //supplierBank.supbank_id = supplierBankDto.supbank_id;
            //supplierBank.supplier_id = supplierBankDto.supplier_id;
            supplierBank.NameBank = supplierBankDto.NameBank;
            supplierBank.Branch = supplierBankDto.Branch;
            supplierBank.AccountNum = supplierBankDto.AccountNum;
            supplierBank.AccountName = supplierBankDto.AccountName;
            supplierBank.SupplierGroup = supplierBankDto.SupplierGroup;
            supplierBank.Company = supplierBankDto.Company;

            _context.SupplierBanks.Update(supplierBank);
            await _context.SaveChangesAsync(cancellationToken);

            return supplierBank;
        }
    }
}