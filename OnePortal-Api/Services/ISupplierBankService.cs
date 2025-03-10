using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public interface ISupplierBankService
    {
        Task<List<SupplierBank>> GetSupplierBankList(CancellationToken cancellationToken = default);
        ValueTask<EntityEntry<SupplierBank>> AddSupplierBank(SupplierBank supplier, CancellationToken cancellationToken = default);
        Task<SupplierBank?> UpdateSupplierBank(int id, SupplierBankDto supplierBankDto, CancellationToken cancellationToken = default);
        Task<SupplierBank> GetSupplierBankByID(int Id, CancellationToken cancellationToken = default);
        Task<List<SupplierBank>> GetSupplierBankBySupplierId (int supplier, CancellationToken cancellationToken = default);
    }
}
