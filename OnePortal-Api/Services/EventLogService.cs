using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public class EventLogService(AppDbContext context) : IEventLogService
    {
        private readonly AppDbContext _context = context;

        public Task<List<EventLogDto>> GetLogByCustomnerId(int customer, CancellationToken cancellationToken = default)
        {
            return _context.EventLogs
                .Where(c => c.CustomerId == customer)
                .Join(_context.User,
                      e => e.UserId,
                      u => u.UserId,
                      (e, u) => new EventLogDto
                      {
                          Id = e.Id,
                          UserId = e.UserId,
                          Username = e.Username,
                          Email = e.Email,
                          Status = e.Status,
                          CustomerId = e.CustomerId,
                          SupplierId = e.SupplierId,
                          Time = e.Time,
                          RejectReason = e.RejectReason,
                          FullName = u.Firstname + " " + u.Lastname,
                          Tel = u.tel,
                          RoleId = u.Role
                      })
                .OrderByDescending(c => c.Time)
                .ToListAsync(cancellationToken);
        }

        public Task<List<EventLogDto>> GetLogBySupplierId(int supplier, CancellationToken cancellationToken = default)
        {
            return _context.EventLogs
                .Where(c => c.SupplierId == supplier)
                .Join(_context.User,
                      e => e.UserId,
                      u => u.UserId,
                      (e, u) => new { EventLog = e, User = u })
                .Join(_context.Supplier,
                      eu => eu.EventLog.SupplierId,
                      s => s.Id,
                      (eu, s) => new EventLogDto
                      {
                          Id = eu.EventLog.Id,
                          UserId = eu.EventLog.UserId,
                          Username = eu.EventLog.Username,
                          Email = eu.EventLog.Email,
                          Status = eu.EventLog.Status,
                          CustomerId = eu.EventLog.CustomerId,
                          SupplierId = eu.EventLog.SupplierId,
                          Time = eu.EventLog.Time,
                          RejectReason = eu.EventLog.RejectReason,
                          FullName = eu.User.Firstname + " " + eu.User.Lastname,
                          Tel = eu.User.tel,
                          RoleId = eu.User.Role,    
                          Payment = s.PaymentMethod

                      })
                .OrderByDescending(c => c.Time)
                .ToListAsync(cancellationToken);
        }


        public ValueTask<EntityEntry<EventLog>> InsertLog(EventLog eventLog, CancellationToken cancellationToken = default)
        {
            return _context.EventLogs.AddAsync(eventLog, cancellationToken);
        }
    }
}
