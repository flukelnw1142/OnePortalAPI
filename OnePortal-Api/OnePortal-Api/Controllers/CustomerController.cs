using Microsoft.AspNetCore.Mvc;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;
using OnePortal_Api.Services;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly AppDbContext _appDbContext;
        private readonly ICustomerTypeMasterDataService _customerTypeMasterDataService;
        public CustomerController(ICustomerService customerService, AppDbContext appDbContext, ICustomerTypeMasterDataService customerTypeMasterDataService)
        {
            _customerService = customerService;
            _appDbContext = appDbContext;
            _customerTypeMasterDataService = customerTypeMasterDataService;
        }

        [HttpGet("CustomerInfo")]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomerInfo()
        {
            return await _customerService.GetCustomerList();
        }

        [HttpPost("AddCustomer")]
        public async Task<ActionResult<Customer>> AddUser(Customer customer)
        {
            _customerService.AddCustomer(customer);
            await _appDbContext.SaveChangesAsync();
            return Ok(new { customer_id = customer.Id, message = "Customer added successfully" });

        }

        [HttpGet("FindCustomerByID")]
        public async Task<ActionResult<Customer>> FindCustomerByID(int id)
        {
            var customer = await _customerService.GetCustomerByID(id);

            if (customer == null)
            {
                return NotFound();
            }

            return customer;
        }

        [HttpPut("UpdateCustomer")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerDto customerDto, CancellationToken cancellationToken)
        {
            if (id != customerDto.Id)
            {
                return BadRequest("Customer ID mismatch");
            }

            var updatedCustomer = await _customerService.UpdateCustomer(id, customerDto, cancellationToken);

            if (updatedCustomer == null)
            {
                return NotFound();
            }

            return Ok(updatedCustomer);
        }

        [HttpGet("GetCustomerType")]
        public async Task<ActionResult<IEnumerable<CustomerTypeMasterData>>> GetCustomerType()
        {
            return await _customerTypeMasterDataService.GetCustomerTypeList();
        }

        [HttpGet("FindCustomerTypeByID")]
        public async Task<ActionResult<CustomerTypeMasterData>> FindCustomerTypeByID(int id)
        {
            var customer = await _customerTypeMasterDataService.GetCustomerTypeByID(id);

            if (customer == null)
            {
                return NotFound();
            }

            return customer;
        }

        [HttpGet("FindCustomerByTypeName")]
        public async Task<ActionResult<Customer>> FindCustomerByTypeName(string customerType)
        {
            var customer = await _customerService.GetCustomerByTypeName(customerType);

            if (customer == null)
            {
                return NotFound();
            }

            return Ok(customer);
        }
        [HttpGet("GetDataByTaxId")]
        public async Task<ActionResult<Customer>> GetDataByTaxId(string taxId)
        {
            var customer = await _customerService.GetDataByTaxID(taxId);

            if (customer == null)
            {
                return NotFound();
            }

            return Ok(customer);
        }

        [HttpGet("FindDataByUserID")]
        public async Task<ActionResult<List<Customer>>> FindDataByUserID(int userid)
        {
            try
            {
                var logs = await _customerService.GetDataByUserId(userid);
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
            var customer = await _customerService.GetDataByUserCompanyACC(company, cancellationToken);
            return Ok(customer);
        }
        [HttpGet("GetDataByUserCompanyFN")]
        public async Task<IActionResult> GetDataByUserCompanyFN(string company, CancellationToken cancellationToken)
        {
            var customer = await _customerService.GetDataByUserCompanyFN(company, cancellationToken);
            return Ok(customer);
        }
    }
}
