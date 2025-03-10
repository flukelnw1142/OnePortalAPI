using Microsoft.AspNetCore.Mvc;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Filters;
using OnePortal_Api.Model;
using OnePortal_Api.Services;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [TypeFilter(typeof(CustomAuthorizationFilter))]
    public class EventLogController(IEventLogService eventLogService, AppDbContext appDbContext) : Controller
    {
        private readonly AppDbContext _appDbContext = appDbContext;
        private readonly IEventLogService _eventLogService = eventLogService;

        [HttpPost("InsertLog")]
        public async Task<ActionResult<EventLog>> InsertLog(EventLog eventLog)
        {
            await _eventLogService.InsertLog(eventLog);
            await _appDbContext.SaveChangesAsync();
            return Ok();

        }


        [HttpGet("FindLogBySupplierID")]
        public async Task<ActionResult<List<EventLogDto>>> GetLogsBySupplierId(int supplierId)
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
            catch (Exception)
            {
                // Log the exception here if needed
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpGet("FindLogByCustomnerID")]
        public async Task<ActionResult<List<EventLogDto>>> FindLogByCustomnerID(int customerId)
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
            catch (Exception)
            {
                // Log the exception here if needed
                return StatusCode(500, "Internal server error");
            }
        }
    }

}
