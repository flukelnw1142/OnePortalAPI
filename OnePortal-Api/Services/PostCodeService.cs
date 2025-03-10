using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public class PostCodeService(AppDbContext context) : IPostCodeServie
    {
        private readonly AppDbContext _context = context;

        public async Task<List<PostCode>> GetPostCodeList(CancellationToken cancellationToken = default)
        {
            return await _context.PostCode
                .FromSqlRaw("EXEC GetPostCodeList")
                .ToListAsync(cancellationToken);
        }

    }
}
