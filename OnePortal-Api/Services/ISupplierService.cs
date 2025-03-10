using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public interface ISupplierService
    {
        Task<List<Supplier>> GetSupplierList(CancellationToken cancellationToken = default);
        ValueTask<EntityEntry<Supplier>> AddSupplier(Supplier supplier, CancellationToken cancellationToken = default);
        Task<Supplier?> UpdateSupplier(int id, SupplierDto supplierDto, CancellationToken cancellationToken = default);
        Task<Supplier> GetSupplierByID(int Id, CancellationToken cancellationToken = default);
        Task<Supplier> GetSupplierByTypeName(string supplier, CancellationToken cancellationToken = default);
        Task<List<Supplier>> GetDataByTaxID(string taxId, CancellationToken cancellationToken = default);
        Task<List<PaymentMethod>> GetPaymentMethodList(CancellationToken cancellationToken = default);
        Task<List<Vat>> GetVatList(CancellationToken cancellationToken = default);
        Task<List<Company>> GetCompanyList(CancellationToken cancellationToken = default);
        Task<List<Supplier>> GetDataByUserId(int userId, CancellationToken cancellationToken = default);
        Task<List<Supplier>> GetDataByUserCompanyACC(string company, int userId, CancellationToken cancellationToken = default);
        Task<List<Supplier>> GetDataByUserCompanyFN(string company, CancellationToken cancellationToken = default);
    }
}
