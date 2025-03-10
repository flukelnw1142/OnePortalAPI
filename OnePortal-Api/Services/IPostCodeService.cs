using OnePortal_Api.Dto;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public interface IPostCodeServie
    {
        Task<List<PostCode>> GetPostCodeList(CancellationToken cancellationToken = default);
    }
}
