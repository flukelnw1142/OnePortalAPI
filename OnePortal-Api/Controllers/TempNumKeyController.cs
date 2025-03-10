using Microsoft.AspNetCore.Mvc;
using OnePortal_Api.Model;
using OnePortal_Api.Services;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TempNumKeyController(ITempNumKeyService tempNumKeyService) : Controller
    {
        private readonly ITempNumKeyService _tempNumKeyService = tempNumKeyService;

        [HttpGet("findbyKey/{id}")]
        public async Task<ActionResult<TempNumKey>> GetMaxNum(string id)
        {
            var key = await _tempNumKeyService.GetMaxNum(id);

            if (key == null)
            {
                return NotFound();
            }

            return key;
        }
    }
}