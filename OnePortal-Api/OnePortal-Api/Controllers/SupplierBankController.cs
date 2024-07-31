using Microsoft.AspNetCore.Mvc;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;
using OnePortal_Api.Services;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierBankController : Controller
    {
        private readonly ISupplierBankService _supplierBankService;
        private readonly AppDbContext _appDbContext;
        public SupplierBankController(ISupplierBankService supplierBankService, AppDbContext appDbContext)
        {
            _supplierBankService = supplierBankService;
            _appDbContext = appDbContext;
        }

        [HttpGet("SupplierBankInfo")]
        public async Task<ActionResult<IEnumerable<SupplierBank>>> GetSupplierBank()
        {
            return await _supplierBankService.GetSupplierBankList();
        }

        [HttpPost("AddSupplierBank")]
        public async Task<ActionResult<SupplierBank>> AddSupplierBank(SupplierBank supplierBank)
        {
            _supplierBankService.AddSupplierBank(supplierBank);
            await _appDbContext.SaveChangesAsync();
            return Ok();

        }

        [HttpPut("UpdateSupplierBank/{id}")]
        public async Task<ActionResult<SupplierBank>> UpdateSupplierBankBySupplierId(int id, SupplierBankDto supplierBankDto)
        {
            var result = await _supplierBankService.UpdateSupplierBank(id, supplierBankDto);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpGet("FindSupplierBankByID")]
        public async Task<ActionResult<SupplierBank>> FindSupplierByID(int id)
        {
            var supplier = await _supplierBankService.GetSupplierBankByID(id);

            if (supplier == null)
            {
                return NotFound();
            }

            return supplier;
        }

        [HttpGet("FindSupplierBankBySupplierID")]
        public async Task<ActionResult<List<SupplierBank>>> FindSupplierBankBySupplierID(int supplierid)
        {
            var banks = await _supplierBankService.GetSupplierBankBySupplierId(supplierid);

            if (banks == null || !banks.Any())
            {
                return NotFound();
            }

            return Ok(banks);
        }
    }
}
