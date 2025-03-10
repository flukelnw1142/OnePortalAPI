using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public interface IBankMasterDataService
    {
        Task<List<BankMasterData>> GetBankMasterDataList(CancellationToken cancellationToken = default);
    }
}
