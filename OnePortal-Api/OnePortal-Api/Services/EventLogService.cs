using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Data;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public class EventLogService : IEventLogService
    {
        private readonly AppDbContext _context;
        public EventLogService(AppDbContext context)
        {
            this._context = context;
        }

        public Task<List<EventLog>> GetLogByCustomnerId(int customer, CancellationToken cancellationToken = default)
        {
            return _context.eventLogs
                    .Where(c => c.customer_id == customer)
                    .OrderByDescending(c => c.time)
                    .ToListAsync(cancellationToken);
        }

        public Task<List<EventLog>> GetLogBySupplierId(int supplier, CancellationToken cancellationToken = default)
        {
            return _context.eventLogs
                    .Where(c => c.supplier_id == supplier)
                    .OrderByDescending(c => c.time)
                    .ToListAsync(cancellationToken);
        }

        public ValueTask<EntityEntry<EventLog>> InsertLog(EventLog eventLog, CancellationToken cancellationToken = default)
        {
            return _context.eventLogs.AddAsync(eventLog, cancellationToken);
        }
    }
}
