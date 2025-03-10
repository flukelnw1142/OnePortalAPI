using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;
using OnePortal_Api.Services;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Text.RegularExpressions;
using Oracle.ManagedDataAccess.Types;
using Newtonsoft.Json;
using System.Text;
using OnePortal_Api.Filters;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public partial class CustomerController(
        ICustomerService customerService,
        AppDbContext appDbContext,
        ICustomerTypeMasterDataService customerTypeMasterDataService,
        IConfiguration configuration,
        IEmailService emailService,
        IWatermarkService watermarkService) : Controller
    {
        private readonly ICustomerService _customerService = customerService;
        private readonly AppDbContext _appDbContext = appDbContext;
        private readonly ICustomerTypeMasterDataService _customerTypeMasterDataService = customerTypeMasterDataService;
        private readonly string _oracleConnectionString = configuration.GetConnectionString("OracleConnection") ?? string.Empty;
        private readonly IEmailService _emailService = emailService;
        private readonly IWatermarkService _watermarkService = watermarkService;

        [HttpGet("CustomerInfo")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<IActionResult> GetCustomerInfo()
        {
            try
            {
                var customers = await _appDbContext.Customer
                    .FromSqlRaw("EXEC GetAllCustomers")
                    .ToListAsync();

                if (customers != null && customers.Count != 0)
                {
                    return Ok(customers);
                }
                else
                {
                    return NotFound("No customer data found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Query failed: {ex.Message}");
            }
        }

        [HttpPost("AddCustomer")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult> AddCustomer(CustomerDto customerDto)
        {
            try
            {
                if (customerDto.UserId == 0)
                {
                    return BadRequest("UserId is required.");
                }

                var parameters = new[]
                {
                    new SqlParameter("@Prefix", customerDto.Prefix ?? (object)DBNull.Value),
                    new SqlParameter("@Name", customerDto.Name ?? (object)DBNull.Value),
                    new SqlParameter("@TaxId", customerDto.TaxId ?? (object)DBNull.Value),
                    new SqlParameter("@AddressSup", customerDto.AddressSup ?? (object)DBNull.Value),
                    new SqlParameter("@District", customerDto.District ?? (object)DBNull.Value),
                    new SqlParameter("@Subdistrict", customerDto.Subdistrict ?? (object)DBNull.Value),
                    new SqlParameter("@Province", customerDto.Province ?? (object)DBNull.Value),
                    new SqlParameter("@PostalCode", customerDto.PostalCode ?? (object)DBNull.Value),
                    new SqlParameter("@Tel", customerDto.Tel ?? (object)DBNull.Value),
                    new SqlParameter("@Email", customerDto.Email ?? (object)DBNull.Value),
                    new SqlParameter("@CustomerNum", customerDto.CustomerNum ?? (object)DBNull.Value),
                    new SqlParameter("@CustomerType", customerDto.CustomerType ?? (object)DBNull.Value),
                    new SqlParameter("@Site", customerDto.Site ?? (object)DBNull.Value),
                    new SqlParameter("@Status", customerDto.Status ?? (object)DBNull.Value),
                    new SqlParameter("@Company", customerDto.Company ?? (object)DBNull.Value),
                    new SqlParameter("@UserId", customerDto.UserId),
                    new SqlParameter("@PostId", customerDto.PostId ?? (object)DBNull.Value),
                    new SqlParameter("@Path", customerDto.Path ?? (object)DBNull.Value),
                    new SqlParameter("@FileReq", customerDto.FileReq ?? (object)DBNull.Value),
                    new SqlParameter("@FileCertificate", customerDto.FileCertificate ?? (object)DBNull.Value),
                    new SqlParameter("@AddressDetail", customerDto.AddressDetail ?? (object)DBNull.Value),
                    new SqlParameter("@LineId", customerDto.LineId ?? (object)DBNull.Value),
                    new SqlParameter("@FileCertificateATR", customerDto.FileCertificateATR ?? (object)DBNull.Value),
                    new SqlParameter("@FileOrther", customerDto.FileOrther ?? (object)DBNull.Value),
                    new SqlParameter("@IsAddressOld", customerDto.IsAddressOld ?? (object)DBNull.Value)
                };

                var connection = _appDbContext.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "EXEC AddCustomer @Prefix, @Name, @TaxId, @AddressSup, @District, @Subdistrict, @Province, @PostalCode, @Tel, @Email, @CustomerNum, @CustomerType, @Site, @Status, @Company, @UserId, @PostId, @Path, @FileReq, @FileCertificate,@AddressDetail,@LineId,@FileCertificateATR,@FileOrther,@IsAddressOld; SELECT SCOPE_IDENTITY()";
                command.Parameters.AddRange(parameters);

                var result = await command.ExecuteScalarAsync();
                var customerId = Convert.ToInt32(result);

                return Ok(new { customer_id = customerId, message = "Customer added successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("FindCustomerByID")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
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
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerDto customerDto, CancellationToken cancellationToken)
        {
            try
            {
                var existingCustomer = await _appDbContext.Customer.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
                if (existingCustomer == null)
                {
                    return NotFound("Customer not found.");
                }

                using var oracleConnection = new OracleConnection(_oracleConnectionString);
                await oracleConnection.OpenAsync(cancellationToken);

                using var oracleCommand = new OracleCommand("CHECK_DUPLICATE_CUSTOMER_WITH_NUM", oracleConnection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                oracleCommand.Parameters.Add("p_site", OracleDbType.Varchar2).Value = customerDto.Site;
                oracleCommand.Parameters.Add("p_name", OracleDbType.Varchar2).Value = customerDto.Name;
                var pCount = new OracleParameter("p_count", OracleDbType.Int32) { Direction = ParameterDirection.Output };
                var pNum = new OracleParameter("p_num", OracleDbType.Varchar2, 200) { Direction = ParameterDirection.Output };

                oracleCommand.Parameters.Add(pCount);
                oracleCommand.Parameters.Add(pNum);

                await oracleCommand.ExecuteNonQueryAsync(cancellationToken);

                string? customerNum = pNum.Value != DBNull.Value && pNum.Value.ToString() != "null"
                                    ? pNum.Value.ToString()
                                    : existingCustomer.CustomerNum ?? string.Empty;

                var oldFiles = await _appDbContext.Customer
                    .Where(c => c.Id == id)
                    .Select(c => new { c.FileReq, c.FileCertificate, c.FileCertificateATR, c.FileOrther })
                    .FirstOrDefaultAsync(cancellationToken);

                if (oldFiles != null)
                {
                    var basePath = Path.Combine("wwwroot", "uploads");
                    if (!string.IsNullOrEmpty(oldFiles.FileReq))
                    {
                        if (customerDto.FileReq == "")
                        {
                            var filePath = Path.Combine(basePath, oldFiles.FileReq);
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(oldFiles.FileCertificate))
                    {
                        if (customerDto.FileCertificate == "")
                        {
                            var filePath = Path.Combine(basePath, oldFiles.FileCertificate);
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(oldFiles.FileCertificateATR))
                    {
                        if (customerDto.FileCertificateATR == "")
                        {
                            var filePath = Path.Combine(basePath, oldFiles.FileCertificateATR);
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(oldFiles.FileOrther))
                    {
                        if (customerDto.FileOrther == "")
                        {
                            var filePath = Path.Combine(basePath, oldFiles.FileOrther);
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }
                    }
                }

                existingCustomer.Prefix = customerDto.Prefix ?? existingCustomer.Prefix;
                existingCustomer.Name = customerDto.Name ?? existingCustomer.Name;
                existingCustomer.TaxId = customerDto.TaxId ?? existingCustomer.TaxId;
                existingCustomer.AddressSup = customerDto.AddressSup ?? existingCustomer.AddressSup;
                existingCustomer.District = customerDto.District ?? existingCustomer.District;
                existingCustomer.Subdistrict = customerDto.Subdistrict ?? existingCustomer.Subdistrict;
                existingCustomer.Province = customerDto.Province ?? existingCustomer.Province;
                existingCustomer.PostalCode = customerDto.PostalCode ?? existingCustomer.PostalCode;
                existingCustomer.Tel = customerDto.Tel ?? existingCustomer.Tel;
                existingCustomer.Email = customerDto.Email ?? existingCustomer.Email;
                existingCustomer.CustomerNum = customerNum ?? string.Empty;
                existingCustomer.CustomerType = customerDto.CustomerType ?? existingCustomer.CustomerType;
                existingCustomer.Site = customerDto.Site ?? existingCustomer.Site;
                existingCustomer.Status = customerDto.Status ?? existingCustomer.Status;
                existingCustomer.Path = customerDto.Path ?? existingCustomer.Path;
                existingCustomer.FileReq = customerDto.FileReq ?? existingCustomer.FileReq;
                existingCustomer.FileCertificate = customerDto.FileCertificate ?? existingCustomer.FileCertificate;
                existingCustomer.Company = customerDto.Company ?? existingCustomer.Company;
                existingCustomer.UserId = customerDto.UserId;
                existingCustomer.AddressDetail = customerDto.AddressDetail ?? existingCustomer.AddressDetail;
                existingCustomer.LineId = customerDto.LineId ?? existingCustomer.LineId;
                existingCustomer.FileCertificateATR = customerDto.FileCertificateATR ?? existingCustomer.FileCertificateATR;
                existingCustomer.FileOrther = customerDto.FileOrther ?? existingCustomer.FileOrther;
                existingCustomer.IsAddressOld = customerDto.IsAddressOld ?? existingCustomer.IsAddressOld;
                var parameters = new[]
                {
                    new SqlParameter("@Id", existingCustomer.Id),
                    new SqlParameter("@Prefix", existingCustomer.Prefix ?? (object)DBNull.Value),
                    new SqlParameter("@Name", existingCustomer.Name ?? (object)DBNull.Value),
                    new SqlParameter("@TaxId", existingCustomer.TaxId ?? (object)DBNull.Value),
                    new SqlParameter("@AddressSup", existingCustomer.AddressSup ?? (object)DBNull.Value),
                    new SqlParameter("@District", existingCustomer.District ?? (object)DBNull.Value),
                    new SqlParameter("@Subdistrict", existingCustomer.Subdistrict ?? (object)DBNull.Value),
                    new SqlParameter("@Province", existingCustomer.Province ?? (object)DBNull.Value),
                    new SqlParameter("@PostalCode", existingCustomer.PostalCode ?? (object)DBNull.Value),
                    new SqlParameter("@Tel", existingCustomer.Tel ?? (object)DBNull.Value),
                    new SqlParameter("@Email", existingCustomer.Email ?? (object)DBNull.Value),
                    new SqlParameter("@CustomerNum", customerNum),
                    new SqlParameter("@CustomerType", existingCustomer.CustomerType ?? (object)DBNull.Value),
                    new SqlParameter("@Site", existingCustomer.Site ?? (object)DBNull.Value),
                    new SqlParameter("@Status", existingCustomer.Status ?? (object)DBNull.Value),
                    new SqlParameter("@Path", existingCustomer.Path ?? (object)DBNull.Value),
                    new SqlParameter("@FileReq", existingCustomer.FileReq ?? (object)DBNull.Value),
                    new SqlParameter("@FileCertificate", existingCustomer.FileCertificate ?? (object)DBNull.Value),
                    new SqlParameter("@Company", existingCustomer.Company ?? (object)DBNull.Value),
                    new SqlParameter("@UserId", existingCustomer.UserId),
                    new SqlParameter("@AddressDetail", existingCustomer.AddressDetail ?? (object)DBNull.Value),
                    new SqlParameter("@LineId", existingCustomer.LineId ?? (object)DBNull.Value),
                    new SqlParameter("@FileCertificateATR", existingCustomer.FileCertificateATR ?? (object)DBNull.Value),
                    new SqlParameter("@FileOrther", existingCustomer.FileOrther ?? (object)DBNull.Value),
                    new SqlParameter("@IsAddressOld", existingCustomer.IsAddressOld ?? (object)DBNull.Value)
                };

                var updatedCustomer = await _appDbContext.Customer
                    .FromSqlRaw("EXEC UpdateCustomer @Id, @Prefix, @Name, @TaxId, @AddressSup, @District, @Subdistrict, @Province, @PostalCode, @Tel, @Email, @CustomerNum, @CustomerType, @Site, @Status, @Path, @FileReq, @FileCertificate, @Company,@UserId, @AddressDetail, @LineId, @FileCertificateATR, @FileOrther, @IsAddressOld", parameters)
                    .ToListAsync(cancellationToken);

                if (updatedCustomer == null || updatedCustomer.Count == 0)
                {
                    return NotFound();
                }

                return Ok(updatedCustomer.First());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetCustomerType")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult<IEnumerable<CustomerTypeMasterData>>> GetCustomerType()
        {
            try
            {
                var customerTypes = await _customerTypeMasterDataService.GetCustomerTypeList();
                if (customerTypes == null || customerTypes.Count == 0)
                {
                    return NotFound("No customer type data found.");
                }

                return Ok(customerTypes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("FindCustomerTypeByID")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult<CustomerTypeMasterData>> FindCustomerTypeByID(int id)
        {
            try
            {
                var customerType = await _customerTypeMasterDataService.GetCustomerTypeByID(id);

                if (customerType == null)
                {
                    return NotFound("Customer type not found.");
                }

                return Ok(customerType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetDataByTaxId")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
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
        [TypeFilter(typeof(CustomAuthorizationFilter))]
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
                return StatusCode(500, $"Internal server error: {ex.Message}. StackTrace: {ex.StackTrace}");
            }
        }

        [HttpGet("GetDataByUserCompanyACC")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<IActionResult> GetDataByUserCompanyACC(string company, int userId, CancellationToken cancellationToken)
        {
            var customer = await _customerService.GetDataByUserCompanyACC(company, userId, cancellationToken);
            return Ok(customer);
        }
        [HttpGet("GetDataByUserCompanyFN")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<IActionResult> GetDataByUserCompanyFN(string company, CancellationToken cancellationToken)
        {
            var customer = await _customerService.GetDataByUserCompanyFN(company, cancellationToken);
            return Ok(customer);
        }

        [HttpGet("FindDataHistoryByUserID")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult<List<CustomerSupplierDto>>> FindDataHistoryByUserID(int? userid, string? company, string? status, string? ownerType, CancellationToken cancellationToken = default)
        {
            try
            {
                var logs = await _customerService.GetCustomerSupplierHistory(
                    userid ?? 0,
                    string.IsNullOrEmpty(company) ? null : company,
                    string.IsNullOrEmpty(status) ? null : status,
                    string.IsNullOrEmpty(ownerType) ? null : ownerType,
                    cancellationToken
                );

                if (logs == null || logs.Count == 0)
                {
                    return NotFound("No data found.");
                }
                return Ok(logs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message} - StackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("FindDataHistoryByApprover")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult<List<CustomerSupplierDto>>> FindDataHistoryByApprover(int? userid, string? company, string status)
        {
            try
            {
                var logs = await _customerService.GetDataHistoryByApprover(userid ?? 0, string.IsNullOrEmpty(company) ? null : company, status);
                if (logs == null || logs.Count == 0)
                {
                    return NotFound("No data found.");
                }
                return Ok(logs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message} - StackTrace: {ex.StackTrace}");

                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("FindDataHistoryByApproverFN")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult<List<CustomerSupplierDto>>> FindDataHistoryByApproverFN(int? userid, string? company, string status)
        {
            try
            {
                var logs = await _customerService.GetDataHistoryByApproverFN(userid ?? 0, string.IsNullOrEmpty(company) ? null : company, status);
                if (logs == null || logs.Count == 0)
                {
                    return NotFound("No data found.");
                }
                return Ok(logs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message} - StackTrace: {ex.StackTrace}");

                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetCustomerSupplierHistory")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult<List<CustomerSupplierDto>>> GetCustomerSupplierHistory(
        int? userId, string? company, string? status, string? ownerType, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _customerService.GetCustomerSupplierHistory(
                    userId ?? 0,
                    string.IsNullOrEmpty(company) ? null : company,
                    string.IsNullOrEmpty(status) ? null : status,
                    string.IsNullOrEmpty(ownerType) ? null : ownerType,
                    cancellationToken
                );

                if (result == null || result.Count == 0)
                {
                    return NotFound("No data found.");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message} - StackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("upload")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file selected for upload.");
            }

            var folderName = "uploads";
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folderName);
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var originalFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadPath, originalFileName);

            string finalFilePath = filePath;

            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            try
            {
                string folderNameC = "";
                if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png")
                {
                    //finalFilePath = await _watermarkService.AddWatermarkToImageCustomer(file, "Confidential", "center", 20);
                }
                else if (fileExtension == ".pdf")
                {
                    //finalFilePath = await _watermarkService.AddWatermarkToPdfCustomer(file, "Confidential", "center", 20);
                    
                    finalFilePath = await _watermarkService.AddWatermarkToPdfAspose(file, "Confidential", folderNameC);
                }
                else
                {
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }

                Console.WriteLine($"File Uploaded: {finalFilePath}");

                return Ok(new { file.FileName, FilePath = finalFilePath.Replace("wwwroot/", "") });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to upload file.", error = ex.Message });
            }
        }


        [GeneratedRegex(@"ORA-20001:\s*(.*)", RegexOptions.Compiled)]
        private static partial Regex OracleErrorMessageRegex();

        [HttpPost("CheckDuplicateCustomer")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<IActionResult> CheckDuplicateCustomer([FromBody] CustomerCheckDto newCustomerDto)
        {
            try
            {
                object? responseFromOracle = null;

                try
                {
                    using var oracleConnection = new OracleConnection(_oracleConnectionString);
                    await oracleConnection.OpenAsync();
                    using var oracleCommand = new OracleCommand("CHECK_DUPLICATE_CUSTOMER_ORACLE_V2", oracleConnection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    oracleCommand.Parameters.Add("p_company", OracleDbType.Varchar2).Value = newCustomerDto.company;
                    oracleCommand.Parameters.Add("p_site", OracleDbType.Varchar2).Value = newCustomerDto.site;
                    oracleCommand.Parameters.Add("p_name", OracleDbType.Varchar2).Value = newCustomerDto.name;

                    var resultCursor = oracleCommand.Parameters.Add("result_cursor", OracleDbType.RefCursor);
                    resultCursor.Direction = ParameterDirection.Output;

                    using var reader = await oracleCommand.ExecuteReaderAsync();

                    if (reader.HasRows)
                    {
                        await reader.ReadAsync();

                        responseFromOracle = new
                        {
                            CustomerName = reader["CUSTOMER_NAME"]?.ToString(),
                            CustomerNumber = reader["CUSTOMER_NUMBER"]?.ToString(),
                            TaxReference = reader["TAX_REFERENCE"]?.ToString(),
                            Address1 = reader["ADDRESS1"]?.ToString(),
                            Address2 = reader["ADDRESS2"]?.ToString(),
                            Address3 = reader["ADDRESS3"]?.ToString(),
                            Address4 = reader["ADDRESS4"]?.ToString(),
                            Province = reader["PROVINCE"]?.ToString(),
                            Postal = reader["POSTAL_CODE"]?.ToString(),
                            Message = "Duplicate customer found: Company, Site, and Name combination already exists."
                        };

                        return StatusCode(409, responseFromOracle); // ส่งสถานะ 409 และข้อมูลจาก Oracle กลับไป
                    }
                    else
                    {
                        // ถ้าไม่มีข้อมูลจาก Stored Procedure แรก ให้เรียก Stored Procedure ตัวที่สอง
                        using var oracleCommandX2 = new OracleCommand("APPS.CHECK_DUPLICATE_CUSTOMER_WITH_NUM_X2", oracleConnection)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        oracleCommandX2.Parameters.Add("p_site", OracleDbType.Varchar2).Value = newCustomerDto.site;
                        oracleCommandX2.Parameters.Add("p_name", OracleDbType.Varchar2).Value = newCustomerDto.name;
                        var resultCursorX2 = oracleCommandX2.Parameters.Add("p_recordset", OracleDbType.RefCursor);
                        resultCursorX2.Direction = ParameterDirection.Output;

                        using var readerX2 = await oracleCommandX2.ExecuteReaderAsync();

                        if (readerX2.HasRows && await readerX2.ReadAsync())
                        {
                            // อ่านข้อมูลจาก SYS_REFCURSOR ของ Stored Procedure ตัวที่สอง
                            responseFromOracle = new
                            {
                                CustomerName = readerX2["CUSTOMER_NAME"]?.ToString(),
                                TaxReference = readerX2["TAX_REFERENCE"]?.ToString(),
                                Address1 = readerX2["ADDRESS1"]?.ToString(),
                                Address2 = readerX2["ADDRESS2"]?.ToString(),
                                Address3 = readerX2["ADDRESS3"]?.ToString(),
                                Address4 = readerX2["ADDRESS4"]?.ToString(),
                                Province = readerX2["PROVINCE"]?.ToString(),
                                Postal = readerX2["POSTAL_CODE"]?.ToString(),
                                Message = "Duplicate customer found in secondary procedure."
                            };

                            return Ok(responseFromOracle);
                        }
                    }
                }
                catch (OracleException ex) when (ex.Number == 20001)
                {
                    var match = OracleErrorMessageRegex().Match(ex.Message);
                    var customMessage = match.Success ? match.Groups[1].Value.Trim() : "An error occurred in Oracle.";
                    return StatusCode(409, customMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Oracle error: {ex.Message}");
                    return StatusCode(500, $"Oracle error: {ex.Message}");
                }

                // ถ้าไม่มีข้อมูลซ้ำใน Oracle ให้ดำเนินการตรวจสอบใน SQL Server ต่อ
                using var sqlConnection = new SqlConnection(_appDbContext.Database.GetConnectionString());
                using var sqlCommand = new SqlCommand("CheckDuplicateCustomer", sqlConnection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                sqlCommand.Parameters.AddWithValue("@Company", newCustomerDto.company);
                sqlCommand.Parameters.AddWithValue("@Site", newCustomerDto.site);
                sqlCommand.Parameters.AddWithValue("@Name", newCustomerDto.name);

                await sqlConnection.OpenAsync();

                try
                {
                    await sqlCommand.ExecuteNonQueryAsync();
                    return Ok();
                }
                catch (SqlException ex) when (ex.Number == 50000)
                {
                    var sqlServerMessage = ex.Message;
                    return Conflict(sqlServerMessage);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


        [HttpPut("UpdateCustomerStatus")]
        public async Task<IActionResult> UpdateCustomerStatus()
        {
            try
            {
                var oracleResults = new List<(int Id, string CustomerNumber)>();
                using (var oracleConnection = new OracleConnection(_oracleConnectionString))
                {
                    using var oracleCommand = new OracleCommand("GetCustomerResults", oracleConnection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    oracleCommand.Parameters.Add("p_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                    await oracleConnection.OpenAsync();
                    using var oracleReader = await oracleCommand.ExecuteReaderAsync();

                    while (oracleReader.Read())
                    {
                        oracleResults.Add((oracleReader.GetInt32(0), oracleReader.GetString(1)));
                    }
                }

                var customerDataTable = new DataTable();
                customerDataTable.Columns.Add("ID", typeof(int));
                customerDataTable.Columns.Add("CUSTOMER_NUMBER", typeof(string));

                foreach (var (id, customerNumber) in oracleResults)
                {
                    customerDataTable.Rows.Add(id, customerNumber);
                }

                var updatedCustomers = new List<CustomerEmailDto>();

                using var sqlConnection = new SqlConnection(_appDbContext.Database.GetConnectionString());
                using var sqlCommand = new SqlCommand("UpdateCustomerStatusByOracleData", sqlConnection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                var batchId = Guid.NewGuid();
                sqlCommand.Parameters.AddWithValue("@BatchId", batchId);

                var parameter = new SqlParameter("@CustomerData", SqlDbType.Structured)
                {
                    TypeName = "dbo.CustomerDataTableType",
                    Value = customerDataTable
                };

                sqlCommand.Parameters.Add(parameter);

                await sqlConnection.OpenAsync();

                using var sqlReader = await sqlCommand.ExecuteReaderAsync();
                while (await sqlReader.ReadAsync())
                {
                    updatedCustomers.Add(new CustomerEmailDto
                    {
                        CustomerId = sqlReader["CustomerId"] != DBNull.Value ? Convert.ToInt32(sqlReader["CustomerId"]) : 0,
                        CustomerNumber = sqlReader["CustomerNumber"]?.ToString() ?? string.Empty,
                        UpdateTimestamp = sqlReader["UpdateTimestamp"] != DBNull.Value ? Convert.ToDateTime(sqlReader["UpdateTimestamp"]) : DateTime.MinValue,
                        UserEmail = sqlReader["UserEmail"]?.ToString() ?? string.Empty,
                        Site = sqlReader["Site"]?.ToString() ?? string.Empty,
                        OwnerAccount = sqlReader["OwnerAccount"]?.ToString() ?? string.Empty,
                        Name = sqlReader["name"]?.ToString() ?? string.Empty,
                        Company = sqlReader["Company"]?.ToString() ?? string.Empty,
                    });
                }

                foreach (var customer in updatedCustomers)
                {
                    var emailRecipients = string.Join(",", new[] { customer.UserEmail, customer.OwnerAccount }
                        .Where(email => !string.IsNullOrWhiteSpace(email))
                        .Distinct());

                    string body = "<p>เรียน ผู้เกี่ยวข้องทุกท่าน</p><br>" +
                    "<p>เรื่อง : Customer ของท่าน Sync. เข้าระบบ ERP Oracle เรียบร้อยแล้ว</p><br>" +
                    "<p>คำขอ Customer ของท่าน นำข้อมูลเข้าระบบ ERP Oracle เรียบร้อยแล้ว</p><br>" +
                    "<p>Customer Number : " + customer.CustomerNumber + "</p>" +
                    "<p>Customer Name : " + customer.Name + "</p>" +
                    "<p>Site  : " + customer.Site + "</p>" +
                    "<p>Company : " + customer.Company + "</p><br>" +
                    "<p>ท่านสามารถติดตามสถานะคำขอของท่าน ได้ที่ " +
                    "<a href='http://10.10.0.28:8085/feature/customer/view/" + customer.CustomerId + "'>ลิงก์นี้</a></p><br>" +
                    "<p>Best Regards,</p><p>OnePortal</p><p>กลุ่มบริษัท เดอะ วัน เอ็นเตอร์ไพรส์ จำกัด (มหาชน)</p>";


                    if (!string.IsNullOrWhiteSpace(emailRecipients))
                    {
                        var recipientList = emailRecipients
                            .Split(',')
                            .Where(email => !string.IsNullOrWhiteSpace(email))
                            .Select(email => new MimeKit.MailboxAddress(email.Trim(), email.Trim()))
                            .ToList();

                        foreach (var recipient in recipientList)
                        {
                            await _emailService.SendEmailAsync(
                                to: recipient.Address,
                                subject: "OnePortal Notification",
                                body: body
                            );
                        }
                    }
                }

                return Ok(updatedCustomers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}