using Microsoft.AspNetCore.Mvc;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;
using OnePortal_Api.Services;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierController : Controller
    {
        private readonly ISupplierService _supplierService;
        private readonly AppDbContext _appDbContext;
        private readonly ISupplierTypeMasterDataService _supplierTypeMasterDataService;
        private readonly IGroupService _groupService;

        public SupplierController(ISupplierService supplierService, AppDbContext appDbContext, ISupplierTypeMasterDataService supplierTypeMasterDataService, IGroupService groupService)
        {
            _supplierService = supplierService;
            _appDbContext = appDbContext;
            _supplierTypeMasterDataService = supplierTypeMasterDataService;
            _groupService = groupService;
        }

        [HttpGet("SupplierInfo")]
        public async Task<ActionResult<IEnumerable<Supplier>>> GetSupplier()
        {
            return await _supplierService.GetSupplierList();
        }

        [HttpPost("AddSupplier")]
        public async Task<ActionResult<Supplier>> AddSupplier(Supplier supplier)
        {
            _supplierService.AddSupplier(supplier);
            await _appDbContext.SaveChangesAsync();
            return Ok(new { supplier_id = supplier.Id,company = supplier.company, message = "Supplier added successfully" });

        }

        [HttpPut("UpdateSupplier")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] SupplierDto supplierDto, CancellationToken cancellationToken)
        {
            if (id != supplierDto.Id)
            {
                return BadRequest("Customer ID mismatch");
            }

            var updateSupplier = await _supplierService.UpdateSupplier(id, supplierDto, cancellationToken);

            if (updateSupplier == null)
            {
                return NotFound();
            }

            return Ok(updateSupplier);
        }

        [HttpGet("FindSupplierByID")]
        public async Task<ActionResult<Supplier>> FindSupplierByID(int id)
        {
            var supplier = await _supplierService.GetSupplierByID(id);

            if (supplier == null)
            {
                return NotFound();
            }

            return supplier;
        }

        [HttpGet("GetSupplierType")]
        public async Task<ActionResult<IEnumerable<SupplierTypeMasterData>>> GetSupplierType()
        {
            return await _supplierTypeMasterDataService.GetSupplierTypeList();
        }

        [HttpGet("FindSupplierTypeByID")]
        public async Task<ActionResult<SupplierTypeMasterData>> FindSupplierTypeByID(int id)
        {
            var supplier = await _supplierTypeMasterDataService.GetSupplierTypeByID(id);

            if (supplier == null)
            {
                return NotFound();
            }

            return supplier;
        }

        [HttpGet("FindSupplierByTypeName")]
        public async Task<ActionResult<Supplier>> FindSupplierByTypeName(string supplierType)
        {
            var customer = await _supplierService.GetSupplierByTypeName(supplierType);

            if (customer == null)
            {
                return NotFound();
            }

            return Ok(customer);
        }

        [HttpGet("GetDataByTaxId")]
        public async Task<ActionResult<Supplier>> GetDataByTaxId(string taxId)
        {
            var customer = await _supplierService.GetDataByTaxID(taxId);

            if (customer == null)
            {
                return NotFound();
            }

            return Ok(customer);
        }

        [HttpGet("PaymentMethodInfo")]
        public async Task<ActionResult<IEnumerable<PaymentMethod>>> GetPaymentMethod()
        {
            return await _supplierService.GetPaymentMethodList();
        }

        [HttpGet("VatInfo")]
        public async Task<ActionResult<IEnumerable<Vat>>> GetVatInfo()
        {
            return await _supplierService.GetVatList();
        }

        [HttpGet("CompanyInfo")]
        public async Task<ActionResult<IEnumerable<Company>>> CompanyInfo()
        {
            return await _supplierService.GetCompanyList();
        }

        [HttpGet("FindDataByUserID")]
        public async Task<ActionResult<List<Supplier>>> FindDataByUserID(int userid)
        {
            try
            {
                var logs = await _supplierService.GetDataByUserId(userid);
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

        [HttpGet("GetDataByUserCompanyACC")]
        public async Task<IActionResult> GetDataByUserCompanyACC(string company, CancellationToken cancellationToken)
        {
            var suppliers = await _supplierService.GetDataByUserCompanyACC(company, cancellationToken);
            return Ok(suppliers);
        }
        [HttpGet("GetDataByUserCompanyFN")]
        public async Task<IActionResult> GetDataByUserCompanyFN(string company, CancellationToken cancellationToken)
        {
            var suppliers = await _supplierService.GetDataByUserCompanyFN(company, cancellationToken);
            return Ok(suppliers);
        }

        [HttpGet("GetGroupNames")]
        public async Task<ActionResult<List<string>>> GetGroupNames([FromQuery] string company)
        {
            Console.WriteLine($"Received company: {company}");
            var groupNames = await _groupService.GetGroupNamesByCompany(company);
            if (groupNames == null || !groupNames.Any())
            {
                return NotFound("No groups found for the specified company.");
            }

            return Ok(groupNames);
        }
    }

}
