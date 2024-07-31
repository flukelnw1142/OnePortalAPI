using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public interface IEventLogService
    {
        ValueTask<EntityEntry<EventLog>> InsertLog(EventLog eventLog, CancellationToken cancellationToken = default);
        Task<List<EventLog>> GetLogBySupplierId(int supplier, CancellationToken cancellationToken = default);

        Task<List<EventLog>> GetLogByCustomnerId(int customer, CancellationToken cancellationToken = default);

    }
}
