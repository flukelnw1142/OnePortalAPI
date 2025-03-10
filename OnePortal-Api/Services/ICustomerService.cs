using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;
using System.Threading.Tasks;

namespace OnePortal_Api.Services
{
    public interface ICustomerService
    {
        Task<List<Customer?>> GetCustomerList(CancellationToken cancellationToken = default);
        ValueTask<EntityEntry<Customer>> AddCustomer(Customer customer, CancellationToken cancellationToken = default);
        Task<Customer?> GetCustomerByID(int Id, CancellationToken cancellationToken = default);
        Task<Customer?> UpdateCustomer(int id, CustomerDto customerDto, CancellationToken cancellationToken = default);
        Task<List<Customer?>> GetDataByTaxID(string taxId, CancellationToken cancellationToken = default);
        Task<List<Customer>> GetDataByUserId(int userid, CancellationToken cancellationToken = default);
        Task<List<Customer>> GetDataByUserCompanyACC(string company, int userId, CancellationToken cancellationToken = default);
        Task<List<Customer?>> GetDataByUserCompanyFN(string company, CancellationToken cancellationToken = default);
        Task<List<CustomerSupplierDto?>> GetDataHistoryByUserId(int userId, string? company);
        Task<List<CustomerSupplierDto?>> GetDataHistoryByApprover(int userId, string? company, string status);
        Task<List<CustomerSupplierDto?>> GetDataHistoryByApproverFN(int userId, string? company, string status);
        Task<List<CustomerSupplierDto?>> GetCustomerSupplierHistory(int userId, string? company, string? status, string? ownerType, CancellationToken cancellationToken = default);
    }
}

