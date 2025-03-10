using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public interface IEventLogService
    {
        ValueTask<EntityEntry<EventLog>> InsertLog(EventLog eventLog, CancellationToken cancellationToken = default);
        Task<List<EventLogDto>> GetLogBySupplierId(int supplier, CancellationToken cancellationToken = default);

        Task<List<EventLogDto>> GetLogByCustomnerId(int customer, CancellationToken cancellationToken = default);

    }
}
