using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public interface ISupplierFileService
    {
        Task AddSupplierFile(SupplierFile supplierFile);
    }
}