using Microsoft.AspNetCore.Mvc;
using OnePortal_Api.Data;
using OnePortal_Api.Model;
using OnePortal_Api.Services;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventLogController : Controller
    {
        private readonly AppDbContext _appDbContext;
        private readonly IEventLogService _eventLogService;
        public EventLogController(IEventLogService eventLogService, AppDbContext appDbContext)
        {
            _eventLogService = eventLogService;
            _appDbContext = appDbContext;
        }

        [HttpPost("InsertLog")]
        public async Task<ActionResult<EventLog>> InsertLog(EventLog eventLog)
        {
            _eventLogService.InsertLog(eventLog);
            await _appDbContext.SaveChangesAsync();
            return Ok();

        }


        [HttpGet("FindLogBySupplierID")]
        public async Task<ActionResult<List<EventLog>>> GetLogsBySupplierId(int supplierId)
        {
            try
            {
                var logs = await _eventLogService.GetLogBySupplierId(supplierId);
                if (logs == null || logs.Count == 0)
                {
                    return NotFound();
                }
                return Ok(logs);
            }
            catch (Exception ex)
            {
                // Log the exception here if needed
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpGet("FindLogByCustomnerID")]
        public async Task<ActionResult<List<EventLog>>> FindLogByCustomnerID(int customerId)
        {
            try
            {
                var logs = await _eventLogService.GetLogByCustomnerId(customerId);
                if (logs == null || logs.Count == 0)
                {
                    return NotFound();
                }
                return Ok(logs);
            }
            catch (Exception ex)
            {
                // Log the exception here if needed
                return StatusCode(500, "Internal server error");
            }
        }
    }

}
