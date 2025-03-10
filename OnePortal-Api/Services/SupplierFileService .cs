using OnePortal_Api.Data;
using OnePortal_Api.Model;
using System.Threading.Tasks;

namespace OnePortal_Api.Services
{
    public class SupplierFileService(AppDbContext context) : ISupplierFileService
    {
        private readonly AppDbContext _context = context;

        public async Task AddSupplierFile(SupplierFile supplierFile)
        {
            await _context.SupplierFiles.AddAsync(supplierFile);
        }
    }
}