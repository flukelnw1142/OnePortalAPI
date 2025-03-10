using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public interface IGroupService
    {
        Task<List<string>> GetGroupNamesByCompany(string company, CancellationToken cancellationToken = default);
    }
}
