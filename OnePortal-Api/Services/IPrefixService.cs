using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public interface IPrefixService
    {
        Task<List<PrefixMasterData>> GetPrefixList(CancellationToken cancellationToken = default);
    }
}
