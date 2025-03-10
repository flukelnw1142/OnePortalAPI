using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public interface ISupplierTypeMasterDataService
    {
        Task<List<SupplierTypeMasterData>> GetSupplierTypeList(CancellationToken cancellationToken = default);
        Task<SupplierTypeMasterData?> GetSupplierTypeByID(int Id, CancellationToken cancellationToken = default);
    }
}