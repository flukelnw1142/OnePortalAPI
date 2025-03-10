using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public interface ITempNumKeyService
    {
        public Task<TempNumKey> GetMaxNum(string id, CancellationToken cancellationToken = default);
    }
}
