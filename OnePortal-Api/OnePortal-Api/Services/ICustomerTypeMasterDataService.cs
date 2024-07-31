using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public interface ICustomerTypeMasterDataService
    {
        Task<List<CustomerTypeMasterData>> GetCustomerTypeList(CancellationToken cancellationToken = default);
        Task<CustomerTypeMasterData> GetCustomerTypeByID(int Id, CancellationToken cancellationToken = default);
    }
}
