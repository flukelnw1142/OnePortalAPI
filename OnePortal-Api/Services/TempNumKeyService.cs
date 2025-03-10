using OnePortal_Api.Data;
using OnePortal_Api.Model;
using Microsoft.EntityFrameworkCore;
namespace OnePortal_Api.Services
{
    public class TempNumKeyService(AppDbContext _context) : ITempNumKeyService
    {
        public async Task<TempNumKey> GetMaxNum(string id, CancellationToken cancellationToken = default)
        {
            var num = await _context.TempNumKeys.Where(e => e.Code == id).FirstOrDefaultAsync(cancellationToken);
            return num ?? throw new KeyNotFoundException("Key Not Found");
        }
    }
}