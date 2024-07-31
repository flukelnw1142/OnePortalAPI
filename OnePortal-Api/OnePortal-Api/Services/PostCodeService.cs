using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public class PostCodeService : IPostCodeServie
    {
        private readonly AppDbContext _context;
        public PostCodeService(AppDbContext context)
        {
            this._context = context;
        }
        public Task<List<PostCode>> GetPostCodeList(CancellationToken cancellationToken = default)
        {
            return _context.PostCode.ToListAsync(cancellationToken);
        }
        public async Task<List<PostCodeDto>> GetPostCodeByPost(CancellationToken cancellationToken = default)
        {
            var postCodeList = await _context.PostCode
                .GroupBy(p => new { p.postalCode, p.district, p.province })
                .Select(g => new PostCodeDto
                {
                    PostalCode = g.Key.postalCode,
                    District = g.Key.district,
                    Province = g.Key.province
                })
                .ToListAsync(cancellationToken);

            return postCodeList;
        }
    }
}
