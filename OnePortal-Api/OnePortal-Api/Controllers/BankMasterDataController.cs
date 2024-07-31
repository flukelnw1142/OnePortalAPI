using Microsoft.AspNetCore.Mvc;
using OnePortal_Api.Data;
using OnePortal_Api.Model;
using OnePortal_Api.Services;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BankMasterDataController : Controller
    {
        private readonly IBankMasterDataService _bankMasterDataService;
        private readonly AppDbContext _appDbContext;
        public BankMasterDataController(IBankMasterDataService bankMasterDataService, AppDbContext appDbContext)
        {
            _bankMasterDataService = bankMasterDataService;
            _appDbContext = appDbContext;
        }

        [HttpGet("BankMasterDataInfo")]
        public async Task<ActionResult<IEnumerable<BankMasterData>>> GetBankMasterDataInfo()
        {
            return await _bankMasterDataService.GetBankMasterDataList();
        }
    }
}
