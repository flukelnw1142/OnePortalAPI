using Dapper;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;
using OnePortal_Api.Services;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Text.RegularExpressions;
using System.Text.Json;
using OnePortal_Api.Filters;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public partial class SupplierController(
        ISupplierService supplierService,
        AppDbContext appDbContext,
        ISupplierTypeMasterDataService supplierTypeMasterDataService,
        IGroupService groupService,
        IConfiguration configuration,
        IEmailService emailService,
        IWatermarkService watermarkService) : Controller
    {
        private readonly ISupplierService _supplierService = supplierService;
        private readonly AppDbContext _appDbContext = appDbContext;
        private readonly ISupplierTypeMasterDataService _supplierTypeMasterDataService = supplierTypeMasterDataService;
        private readonly IGroupService _groupService = groupService;
        private readonly string _oracleConnectionString = configuration.GetConnectionString("OracleConnection") ?? string.Empty;
        private readonly IEmailService _emailService = emailService;
        private readonly IWatermarkService _watermarkService = watermarkService;

        [HttpGet("SupplierInfo")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<IActionResult> GetSupplier()
        {
            try
            {
                var suppliers = await _appDbContext.Supplier
                    .FromSqlRaw("EXEC GetAllSuppliers")
                    .ToListAsync();

                if (suppliers != null && suppliers.Count != 0)
                {
                    return Ok(suppliers);
                }
                else
                {
                    return NotFound("No supplier data found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Query failed: {ex.Message}");
            }
        }

        [HttpPost("AddSupplier")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult<Supplier>> AddSupplier(Supplier supplier)
        {
            await _supplierService.AddSupplier(supplier);
            await _appDbContext.SaveChangesAsync();
            return Ok(new { supplier_id = supplier.Id, supplier.Company, message = "Supplier added successfully" });
        }

        [HttpPost("AddSupplierWithFiles")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult> AddSupplierWithFiles([FromForm] SupplierWithFilesDto supplierWithFilesDto)
        {
            try
            {
                var labelTexts = JsonConvert.DeserializeObject<List<string>>(supplierWithFilesDto.LabelTextsJson);
                if (labelTexts != null)
                {
                    supplierWithFilesDto.LabelTexts = labelTexts;
                }
                else
                {
                    supplierWithFilesDto.LabelTexts = [];
                }

                var connectionString = _appDbContext.Database.GetConnectionString();

                var filesTable = new DataTable();
                filesTable.Columns.Add("FileType", typeof(string));
                filesTable.Columns.Add("GroupName", typeof(string));
                filesTable.Columns.Add("FilePath", typeof(string));
                filesTable.Columns.Add("FileName", typeof(string));
                filesTable.Columns.Add("LabelText", typeof(string));
                filesTable.Columns.Add("UploadedDate", typeof(DateTime));
                filesTable.Columns.Add("UploadedBy", typeof(int));

                if (supplierWithFilesDto.LabelTexts != null && supplierWithFilesDto.Files != null)
                {
                    if (supplierWithFilesDto.Files != null && supplierWithFilesDto.Files.Count > 0)
                    {
                        for (int i = 0; i < supplierWithFilesDto.Files.Count; i++)
                        {
                            var file = supplierWithFilesDto.Files[i];
                            if (file.Length > 0)
                            {
                                var uploadsFolder = Path.Combine("uploads", "suppliers");
                                if (!Directory.Exists(uploadsFolder))
                                {
                                    Directory.CreateDirectory(uploadsFolder);
                                }

                                var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                var labelText = supplierWithFilesDto.LabelTexts.Count > i
                                    ? supplierWithFilesDto.LabelTexts[i]
                                    : string.Empty;

                                filesTable.Rows.Add(file.ContentType, supplierWithFilesDto.GroupName, filePath, file.FileName, labelText, DateTime.Now, supplierWithFilesDto.UploadedBy);
                            }
                        }
                    }
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    using var command = new SqlCommand("AddSupplierWithFiles", connection);
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@Prefix", supplierWithFilesDto.Prefix);
                    command.Parameters.AddWithValue("@Name", supplierWithFilesDto.Name);
                    command.Parameters.AddWithValue("@TaxId", supplierWithFilesDto.Tax_Id);
                    command.Parameters.AddWithValue("@AddressSup", supplierWithFilesDto.AddressSup);
                    command.Parameters.AddWithValue("@District", supplierWithFilesDto.District);
                    command.Parameters.AddWithValue("@Subdistrict", supplierWithFilesDto.Subdistrict);
                    command.Parameters.AddWithValue("@Province", supplierWithFilesDto.Province);
                    command.Parameters.AddWithValue("@PostalCode", supplierWithFilesDto.PostalCode);
                    command.Parameters.AddWithValue("@Tel", supplierWithFilesDto.Tel);
                    command.Parameters.AddWithValue("@Email", supplierWithFilesDto.Email);
                    command.Parameters.AddWithValue("@SupplierType", supplierWithFilesDto.SupplierType);
                    command.Parameters.AddWithValue("@Site", supplierWithFilesDto.Site);
                    command.Parameters.AddWithValue("@Status", supplierWithFilesDto.Status);
                    command.Parameters.AddWithValue("@PaymentMethod", supplierWithFilesDto.PaymentMethod);
                    command.Parameters.AddWithValue("@UserId", supplierWithFilesDto.UserId);
                    command.Parameters.AddWithValue("@Company", supplierWithFilesDto.Company);
                    command.Parameters.AddWithValue("@Type", supplierWithFilesDto.Type);
                    command.Parameters.AddWithValue("@Mobile", supplierWithFilesDto.Mobile);
                    command.Parameters.AddWithValue("@postId", supplierWithFilesDto.PostId);

                    var filesParameter = new SqlParameter("@Files", SqlDbType.Structured)
                    {
                        TypeName = "dbo.FileTableType",
                        Value = filesTable
                    };
                    command.Parameters.Add(filesParameter);

                    await connection.OpenAsync();
                    using var reader = await command.ExecuteReaderAsync();

                    if (reader.Read())
                    {
                        var insertedSupplierId = reader.GetInt32(reader.GetOrdinal("SupplierId"));
                        var company = reader.GetString(reader.GetOrdinal("Company"));

                        return Ok(new { supplier_id = insertedSupplierId, company, message = "Supplier added successfully" });
                    }
                }

                return BadRequest("Unable to insert supplier.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{ex.Message}");
            }
        }

        [HttpPost("AddOrUpdateSupplierWithFiles/{id?}")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<IActionResult> AddOrUpdateSupplierWithFiles(
        int? id,
        [FromForm] SupplierWithFilesDto supplierWithFilesDto,
        [FromForm] List<IFormFile> files,
        [FromForm] string fileIdsJson,
        [FromForm] string fileIdsToRemoveJson,
        CancellationToken cancellationToken)
        {
            try
            {
                if (id.HasValue)
                {
                    supplierWithFilesDto.Id = id.Value;
                }

                if (files != null && files.Count > 0)
                {
                    supplierWithFilesDto.Files = files;
                }

                var labelTexts = JsonConvert.DeserializeObject<List<string>>(supplierWithFilesDto.LabelTextsJson) ?? [];
                supplierWithFilesDto.LabelTexts = labelTexts;

                var connectionString = _appDbContext.Database.GetConnectionString();
                var filesTable = new DataTable();
                filesTable.Columns.Add("FileType", typeof(string));
                filesTable.Columns.Add("GroupName", typeof(string));
                filesTable.Columns.Add("FilePath", typeof(string));
                filesTable.Columns.Add("FileName", typeof(string));
                filesTable.Columns.Add("LabelText", typeof(string));
                filesTable.Columns.Add("UploadedDate", typeof(DateTime));
                filesTable.Columns.Add("UploadedBy", typeof(int));

                var fileIds = JsonConvert.DeserializeObject<List<int?>>(fileIdsJson)?
                                .Where(id => id.HasValue)
                                .Select(id => id.GetValueOrDefault())
                                .ToList() ?? [];

                var fileIdsToRemove = JsonConvert.DeserializeObject<List<int>>(fileIdsToRemoveJson) ?? [];

                if (fileIdsToRemove.Count > 0)
                {
                    using var connection = new SqlConnection(connectionString);
                    using var deleteCommand = new SqlCommand("GetFilePathsFromSupplierFiles", connection);
                    deleteCommand.CommandType = CommandType.StoredProcedure;

                    var fileIdsToRemoveParameter = new SqlParameter("@FileIds", SqlDbType.Structured)
                    {
                        TypeName = "dbo.INTTableType",
                        Value = CreateIntDataTable(fileIdsToRemove)
                    };
                    deleteCommand.Parameters.Add(fileIdsToRemoveParameter);

                    await connection.OpenAsync(cancellationToken);
                    var reader = await deleteCommand.ExecuteReaderAsync(cancellationToken);

                    var filesToRemove = new List<string>();
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        filesToRemove.Add(reader.GetString(0));
                    }
                    reader.Close();

                    foreach (var filePath in filesToRemove)
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                }

                //if (supplierWithFilesDto.Files != null && supplierWithFilesDto.Files.Count > 0)
                //{
                //    for (int i = 0; i < supplierWithFilesDto.Files.Count; i++)
                //    {
                //        var file = supplierWithFilesDto.Files[i];
                //        if (file.Length > 0)
                //        {
                //            var uploadsFolder = Path.Combine("uploads", "suppliers");
                //            if (!Directory.Exists(uploadsFolder))
                //            {
                //                Directory.CreateDirectory(uploadsFolder);
                //            }

                //            string outputFilePath = "";

                //            // 📌 **ตรวจสอบประเภทไฟล์**
                //            var fileExtension = Path.GetExtension(file.FileName).ToLower();
                //            if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png" || fileExtension == ".gif")
                //            {
                //                // 🖼 **เพิ่มลายน้ำในรูปภาพ**
                //                outputFilePath = await _watermarkService.AddWatermarkToImage(file, "Confidential", "bottom_right", 150);
                //            }
                //            else if (fileExtension == ".pdf")
                //            {
                //                // 📄 **เพิ่มลายน้ำใน PDF**
                //                outputFilePath = await _watermarkService.AddWatermarkToPdf(file, "Confidential", "center", 100);
                //            }
                //            else
                //            {
                //                // 🔴 ถ้าเป็นไฟล์อื่นที่ไม่รองรับ ให้บันทึกตรงๆ
                //                outputFilePath = Path.Combine(uploadsFolder, $"{Guid.NewGuid()}_{file.FileName}");
                //                using var fileStream = new FileStream(outputFilePath, FileMode.Create);
                //                await file.CopyToAsync(fileStream, cancellationToken);
                //            }

                //            var labelText = supplierWithFilesDto.LabelTexts.Count > i ? supplierWithFilesDto.LabelTexts[i] : string.Empty;

                //            filesTable.Rows.Add(file.ContentType, supplierWithFilesDto.GroupName, outputFilePath, file.FileName, labelText, DateTime.Now, supplierWithFilesDto.UploadedBy);
                //        }
                //    }
                //}

                using (var connection = new SqlConnection(connectionString))
                {
                    using var command = new SqlCommand("AddOrUpdateSupplierWithFilesV4", connection);
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@SupplierId", supplierWithFilesDto.Id ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Prefix", supplierWithFilesDto.Prefix ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Name", supplierWithFilesDto.Name ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@TaxId", supplierWithFilesDto.Tax_Id ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@AddressSup", supplierWithFilesDto.AddressSup ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@District", supplierWithFilesDto.District ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Subdistrict", supplierWithFilesDto.Subdistrict ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Province", supplierWithFilesDto.Province ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PostalCode", supplierWithFilesDto.PostalCode ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Tel", supplierWithFilesDto.Tel ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Email", supplierWithFilesDto.Email ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@SupplierType", supplierWithFilesDto.SupplierType ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Site", supplierWithFilesDto.Site ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Status", supplierWithFilesDto.Status ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Vat", supplierWithFilesDto.Vat ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PaymentMethod", supplierWithFilesDto.PaymentMethod ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@UserId", supplierWithFilesDto.UserId);
                    command.Parameters.AddWithValue("@Company", supplierWithFilesDto.Company ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Type", supplierWithFilesDto.Type ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Mobile", supplierWithFilesDto.Mobile ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@postId", supplierWithFilesDto.PostId ?? (object)DBNull.Value);

                    var filesParameter = new SqlParameter("@Files", SqlDbType.Structured)
                    {
                        TypeName = "dbo.FileTableType",
                        Value = filesTable
                    };
                    command.Parameters.Add(filesParameter);

                    command.Parameters.AddWithValue("@FileIds", fileIdsJson ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@FileIdsToRemove", fileIdsToRemoveJson ?? (object)DBNull.Value);

                    await connection.OpenAsync(cancellationToken);
                    using var resultReader = await command.ExecuteReaderAsync(cancellationToken);

                    if (resultReader.Read())
                    {
                        var supplierId = resultReader.GetInt32(resultReader.GetOrdinal("SupplierId"));
                        var company = resultReader.GetString(resultReader.GetOrdinal("Company"));

                        return Ok(new { supplier_id = supplierId, company, message = "Supplier added or updated successfully" });
                    }
                }

                return BadRequest("Unable to insert or update supplier.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("SaveSupplierBankWithFiles")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<IActionResult> SaveSupplierBankWithFiles(
        [FromForm] SupplierWithFilesDto supplierWithFilesDto,
        [FromForm] List<IFormFile> files,
        [FromForm] string fileIdsToRemoveJson,
        CancellationToken cancellationToken)
        {
            try
            {
                var supplierBanksTable = CreateSupplierBankDataTable(supplierWithFilesDto.SupplierBanks);
                var labelTexts = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(supplierWithFilesDto.LabelTextsJson ?? "{}") ?? [];
                var fileIdsToRemove = JsonConvert.DeserializeObject<List<SupplierBankFileRemovalDto>>(fileIdsToRemoveJson ?? "[]") ?? [];
                var filesTable = CreateFilesDataTable(files, supplierWithFilesDto.SupplierBanks, fileIdsToRemove, labelTexts);
                var fileIdsToRemoveTable = CreateFileIdsToRemoveDataTable(fileIdsToRemove);


                var connectionString = _appDbContext.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                using var command = new SqlCommand("AddOrUpdateSupplierBankWithFilesV5", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.Add(new SqlParameter("@SupplierBanks", SqlDbType.Structured) { TypeName = "dbo.SupplierBankTableType", Value = supplierBanksTable });
                command.Parameters.Add(new SqlParameter("@Files", SqlDbType.Structured) { TypeName = "dbo.FileTableType", Value = filesTable });
                command.Parameters.Add(new SqlParameter("@FileIdsToRemove", SqlDbType.Structured) { TypeName = "dbo.SupplierBankFileRemovalTableType", Value = fileIdsToRemoveTable });

                await connection.OpenAsync(cancellationToken);
                await command.ExecuteNonQueryAsync(cancellationToken);

                return Ok(new { message = "Supplier bank data and files saved successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPost("SaveSupplierWithBankAndFiles/{isBank:bool}/{id?}")]
        [RequestSizeLimit(100_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<IActionResult> SaveSupplierWithBankAndFiles(
        bool isBank,
        int? id,
        [FromForm] string supplierJson,
        [FromForm] List<IFormFile> files,
        [FromForm] string? fileIdsJson = null,
        [FromForm] string? fileIdsToRemoveJson = null,
        [FromForm] string? labelTextsJson = null,
        CancellationToken cancellationToken = default)
        {
            try
            {
                const long maxFileSize = 100 * 1024 * 1024;

                foreach (var file in files)
                {
                    if (file.Length > maxFileSize)
                    {
                        return BadRequest(new { message = $"File '{file.FileName}' exceeds the maximum allowed size of {maxFileSize / (1024 * 1024)} MB." });
                    }
                }

                if (string.IsNullOrWhiteSpace(supplierJson))
                {
                    return BadRequest(new { message = "No JSON data received." });
                }

                var labelTexts = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(labelTextsJson ?? "{}") ?? [];

                if (isBank)
                {
                    var supplierBanks = JsonConvert.DeserializeObject<List<SupplierBankWithFilesDto>>(supplierJson);
                    if (supplierBanks == null || supplierBanks.Count == 0)
                    {
                        return BadRequest(new { message = "No valid supplier bank data found in JSON." });
                    }

                    await ProcessSupplierBankData(supplierBanks, files, fileIdsJson ?? string.Empty, fileIdsToRemoveJson ?? string.Empty, labelTexts, cancellationToken);
                }
                else
                {
                    var supplierWithFilesDto = JsonConvert.DeserializeObject<SupplierWithFilesDto>(supplierJson);
                    if (supplierWithFilesDto == null)
                    {
                        return BadRequest(new { message = "No valid supplier data found in JSON." });
                    }

                    supplierWithFilesDto.Id = id;
                    var parsedLabelTexts = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(supplierWithFilesDto.LabelTextsJson ?? "{}")
                                           ?? [];

                    await ProcessSupplierData(supplierWithFilesDto, files, fileIdsToRemoveJson ?? string.Empty, parsedLabelTexts, cancellationToken);
                }

                return Ok(new { message = "Supplier or Supplier Bank data and files saved successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        private List<SupplierBankFileTemplateDto> GetSupplierBankFileTemplates()
        {
            var templates = new List<SupplierBankFileTemplateDto>();

            using (var connection = new SqlConnection(_appDbContext.Database.GetConnectionString()))
            {
                connection.Open();
                templates = connection.Query<SupplierBankFileTemplateDto>("EXEC GetSupplierBankFileTemplates").ToList();
            }

            return templates;
        }

        private DataTable CreateFilesDataTable(
        List<IFormFile> files,
        List<SupplierBankWithFilesDto> supplierBanks,
        List<SupplierBankFileRemovalDto> fileIdsToRemove,
        Dictionary<string, List<string>> labelTexts)
        {
            var table = new DataTable();
            table.Columns.Add("FileType", typeof(string));
            table.Columns.Add("GroupName", typeof(string));
            table.Columns.Add("FilePath", typeof(string));
            table.Columns.Add("FileName", typeof(string));
            table.Columns.Add("LabelText", typeof(string));
            table.Columns.Add("UploadedDate", typeof(DateTime));
            table.Columns.Add("UploadedBy", typeof(int));

            var labelEntries = labelTexts
                .SelectMany(entry => entry.Value.Select(label => new { GroupName = entry.Key.Replace(" ", "").ToUpper(), Label = label }))
                .ToList();

            var templates = GetSupplierBankFileTemplates();

            int labelIndex = 0;

            foreach (var bank in supplierBanks)
            {
                var supbankId = bank.SupbankId ?? 0;
                var supplierGroup = (bank.SupplierGroup ?? "default").Replace(" ", "").ToUpper();

                var relevantTemplates = templates
                    .Where(template => template.GroupName.Replace(" ", "").ToUpper().Equals(supplierGroup))
                    .ToList();

                var relevantLabels = labelEntries
                    .Where(entry => relevantTemplates.Any(template => template.LabelText.Equals(entry.Label, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                bool isNewUploadOnly = fileIdsToRemove == null || fileIdsToRemove.Count == 0;

                if (isNewUploadOnly && files.Count > 0)
                {
                    foreach (var labelEntry in relevantLabels)
                    {
                        if (labelIndex >= files.Count) break;

                        var file = files[labelIndex];
                        var uploadsFolder = Path.Combine("uploads", bank.SupplierGroup ?? "default");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            file.CopyTo(stream);
                        }

                        var fileType = file.ContentType;
                        var labelText = labelEntry.Label;

                        table.Rows.Add(fileType, bank.SupplierGroup, filePath, file.FileName, labelText, DateTime.Now, bank.UploadedBy);
                        labelIndex++;
                    }
                }
            }

            return table;
        }

        private async Task ProcessSupplierData(
            SupplierWithFilesDto supplierWithFilesDto,
            List<IFormFile> files,
            string fileIdsToRemoveJson,
            Dictionary<string, List<string>> labelTexts,
            CancellationToken cancellationToken)
        {
            var connectionString = _appDbContext.Database.GetConnectionString();

            var fileIdsToRemove = JsonConvert.DeserializeObject<List<SupplierBankFileRemovalDto>>(fileIdsToRemoveJson ?? "[]") ?? [];

            var filesTable = CreateFilesDataTable(files, supplierWithFilesDto.SupplierBanks, fileIdsToRemove, labelTexts);

            using var connection = new SqlConnection(connectionString);
            using var command = new SqlCommand("AddOrUpdateSupplierWithFilesV4", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@SupplierId", supplierWithFilesDto.Id ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Prefix", supplierWithFilesDto.Prefix ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Name", supplierWithFilesDto.Name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@TaxId", supplierWithFilesDto.Tax_Id ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@AddressSup", supplierWithFilesDto.AddressSup ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@District", supplierWithFilesDto.District ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Subdistrict", supplierWithFilesDto.Subdistrict ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Province", supplierWithFilesDto.Province ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@PostalCode", supplierWithFilesDto.PostalCode ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Tel", supplierWithFilesDto.Tel ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Email", supplierWithFilesDto.Email ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@SupplierType", supplierWithFilesDto.SupplierType ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Site", supplierWithFilesDto.Site ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Status", supplierWithFilesDto.Status ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Vat", supplierWithFilesDto.Vat ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@PaymentMethod", supplierWithFilesDto.PaymentMethod ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@UserId", supplierWithFilesDto.UserId);
            command.Parameters.AddWithValue("@Company", supplierWithFilesDto.Company ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Type", supplierWithFilesDto.Type ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Mobile", supplierWithFilesDto.Mobile ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@OwnerAcc", supplierWithFilesDto.OwnerAcc ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@OwnerFn", supplierWithFilesDto.OwnerFn ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@PostId", supplierWithFilesDto.PostId ?? (object)DBNull.Value);

            var filesParameter = new SqlParameter("@Files", SqlDbType.Structured)
            {
                TypeName = "dbo.FileTableType",
                Value = filesTable
            };
            command.Parameters.Add(filesParameter);

            command.Parameters.AddWithValue("@FileIdsToRemove", fileIdsToRemoveJson ?? (object)DBNull.Value);

            await connection.OpenAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task ProcessSupplierBankData(
        List<SupplierBankWithFilesDto> supplierBanks,
        List<IFormFile> files,
        string fileIdsJson,
        string fileIdsToRemoveJson,
        Dictionary<string, List<string>> labelTexts,
        CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(fileIdsJson);

            var connectionString = _appDbContext.Database.GetConnectionString();

            var fileIdsToRemove = JsonConvert.DeserializeObject<List<SupplierBankFileRemovalDto>>(fileIdsToRemoveJson ?? "[]") ?? [];
            var fileIdList = fileIdsToRemove.Select(dto => dto.FileId).ToList();

            DeleteFilesFromDatabaseAndDisk(fileIdList, "SupplierBankFiles");

            var supplierBankTable = CreateSupplierBankDataTable(supplierBanks);
            var filesTable = CreateFilesDataTable(files, supplierBanks, fileIdsToRemove, labelTexts);

            using var connection = new SqlConnection(connectionString);
            using var command = new SqlCommand("AddOrUpdateSupplierBankWithFilesV5", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@SupplierBanks", SqlDbType.Structured) { TypeName = "dbo.SupplierBankTableType", Value = supplierBankTable });
            command.Parameters.Add(new SqlParameter("@Files", SqlDbType.Structured) { TypeName = "dbo.FileTableType", Value = filesTable });

            if (fileIdsToRemove.Count > 0)
            {
                command.Parameters.Add(new SqlParameter("@FileIdsToRemove", SqlDbType.Structured)
                {
                    TypeName = "dbo.SupplierBankFileRemovalTableType",
                    Value = CreateFileIdsToRemoveDataTable(fileIdsToRemove)
                });
            }

            await connection.OpenAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static void DeleteFilesFromDisk(List<string> filePaths)
        {
            if (filePaths == null || filePaths.Count == 0) return;

            foreach (var filePath in filePaths)
            {
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                        Console.WriteLine($"Deleted file: {filePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting file at {filePath}: {ex.Message}");
                    }
                }
            }
        }

        private string GetFilePathById(int fileId, string tableName)
        {
            using var connection = new SqlConnection(_appDbContext.Database.GetConnectionString());
            var query = $"SELECT FilePath FROM {tableName} WHERE Id = @FileId";

            try
            {
                connection.Open();
                var filePath = connection.QueryFirstOrDefault<string>(query, new { FileId = fileId });
                return filePath ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving file path for FileId {fileId} in {tableName}: {ex.Message}");
                return string.Empty;
            }
        }

        private void DeleteFilesFromDatabaseAndDisk(List<int> fileIdsToRemove, string tableName)
        {
            var filePaths = fileIdsToRemove
                .Select(id => GetFilePathById(id, tableName))
                .Where(path => !string.IsNullOrEmpty(path))
                .ToList();

            DeleteFilesFromDisk(filePaths);
        }

        [HttpPut("UpdateSupplier")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
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

        [HttpPut("UpdateSupplierWithFiles")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<IActionResult> UpdateSupplierWithFiles(int id, [FromForm] SupplierDto supplierDto, [FromForm] List<IFormFile> files, [FromForm] string fileIdsJson, [FromForm] string fileIdsToRemoveJson, [FromForm] string labelTextsJson, CancellationToken cancellationToken)
        {
            try
            {
                var connectionString = _appDbContext.Database.GetConnectionString();
                var filesTable = new DataTable();
                filesTable.Columns.Add("FileType", typeof(string));
                filesTable.Columns.Add("GroupName", typeof(string));
                filesTable.Columns.Add("FilePath", typeof(string));
                filesTable.Columns.Add("FileName", typeof(string));
                filesTable.Columns.Add("LabelText", typeof(string));
                filesTable.Columns.Add("UploadedDate", typeof(DateTime));
                filesTable.Columns.Add("FileId", typeof(int));

                var fileIds = JsonConvert.DeserializeObject<List<int?>>(fileIdsJson)?
                                .Where(id => id.HasValue)
                                .Select(id => id.GetValueOrDefault())
                                .ToList() ?? [];

                var fileIdsToRemove = JsonConvert.DeserializeObject<List<int>>(fileIdsToRemoveJson) ?? [];
                var labelTexts = JsonConvert.DeserializeObject<List<string>>(labelTextsJson) ?? [];

                if (fileIdsToRemove.Count > 0)
                {
                    using var connection = new SqlConnection(connectionString);
                    using var deleteCommand = new SqlCommand("GetFilePathsFromSupplierFiles", connection);
                    deleteCommand.CommandType = CommandType.StoredProcedure;

                    var fileIdsToRemoveParameter = new SqlParameter("@FileIds", SqlDbType.Structured)
                    {
                        TypeName = "dbo.INTTableType",
                        Value = CreateIntDataTable(fileIdsToRemove)
                    };
                    deleteCommand.Parameters.Add(fileIdsToRemoveParameter);

                    await connection.OpenAsync(cancellationToken);
                    var reader = await deleteCommand.ExecuteReaderAsync(cancellationToken);

                    var filesToRemove = new List<string>();
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        filesToRemove.Add(reader.GetString(0));
                    }

                    reader.Close();

                    foreach (var filePath in filesToRemove)
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                }

                if (files != null && files.Count > 0)
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        var file = files[i];
                        if (file.Length > 0)
                        {
                            var uploadsFolder = Path.Combine("uploads", "suppliers");
                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream, cancellationToken);
                            }

                            var groupName = "SupplierFile";
                            var labelText = labelTexts.Count > i ? labelTexts[i] : string.Empty;

                            filesTable.Rows.Add(file.ContentType, groupName, filePath, file.FileName, labelText, DateTime.Now, (fileIds.Count > i ? fileIds[i] : 0));
                        }
                    }
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    using var command = new SqlCommand("UpdateSupplierWithFilesV3", connection);
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@SupplierId", id);
                    command.Parameters.AddWithValue("@Prefix", supplierDto.Prefix ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Name", supplierDto.Name ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@TaxId", supplierDto.Tax_Id ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@AddressSup", supplierDto.AddressSup ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@District", supplierDto.District ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Subdistrict", supplierDto.Subdistrict ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Province", supplierDto.Province ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PostalCode", supplierDto.PostalCode ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Tel", supplierDto.Tel ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Email", supplierDto.Email ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@SupplierNum", supplierDto.SupplierNum ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@SupplierType", supplierDto.SupplierType ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Site", supplierDto.Site ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Status", supplierDto.Status ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Vat", supplierDto.Vat ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PaymentMethod", supplierDto.PaymentMethod ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@UserId", supplierDto.UserId);
                    command.Parameters.AddWithValue("@Company", supplierDto.Company ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Type", supplierDto.Type ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Mobile", supplierDto.Mobile ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@postId", supplierDto.PostId ?? (object)DBNull.Value);

                    if (supplierDto.Status == "Approved By ACC" || supplierDto.Status == "Reject By ACC")
                    {
                        command.Parameters.AddWithValue("@OwnerAcc", supplierDto.UserId);
                    }
                    else if (supplierDto.Status == "Approved By FN" || supplierDto.Status == "Reject By FN")
                    {
                        command.Parameters.AddWithValue("@OwnerFN", supplierDto.UserId);
                    }

                    var filesParameter = new SqlParameter("@Files", SqlDbType.Structured)
                    {
                        TypeName = "dbo.FileTableType",
                        Value = filesTable
                    };
                    command.Parameters.Add(filesParameter);

                    command.Parameters.AddWithValue("@FileIds", fileIdsJson ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@FileIdsToRemove", fileIdsToRemoveJson ?? (object)DBNull.Value);

                    await connection.OpenAsync(cancellationToken);
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                return Ok(new { supplier_id = id, supplierDto.Company, message = "Supplier updated successfully with files." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        private static DataTable CreateIntDataTable(List<int> values)
        {
            var table = new DataTable();
            table.Columns.Add("FileId", typeof(int));

            foreach (var value in values)
            {
                var row = table.NewRow();
                row["FileId"] = value;
                table.Rows.Add(row);
            }

            return table;
        }

        [HttpGet("FindSupplierByID")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult<Supplier>> FindSupplierByID(int id)
        {
            var supplier = await _supplierService.GetSupplierByID(id);

            if (supplier == null)
            {
                return NotFound();
            }

            return supplier;
        }

        [HttpGet("FindSupplierByIDV2")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult<SupplierWithFilesDto>> FindSupplierByIDV2(int id)
        {
            try
            {
                var connectionString = _appDbContext.Database.GetConnectionString();

                SupplierWithFilesDto supplierWithFiles = new()
                {
                    Prefix = string.Empty,
                    Name = string.Empty,
                    Tax_Id = string.Empty,
                    AddressSup = string.Empty,
                    District = string.Empty,
                    Subdistrict = string.Empty,
                    Province = string.Empty,
                    PostalCode = string.Empty,
                    Tel = string.Empty,
                    Email = string.Empty,
                    SupplierNum = string.Empty,
                    SupplierType = string.Empty,
                    Site = string.Empty,
                    Vat = string.Empty,
                    Status = string.Empty,
                    PaymentMethod = string.Empty,
                    Company = string.Empty,
                    Type = string.Empty,
                    Mobile = string.Empty,
                    Firstname = string.Empty,
                    Lastname = string.Empty,
                    EmailUser = string.Empty,
                    Telephone = string.Empty,
                    SupplierFiles = []
                };

                using (var connection = new SqlConnection(connectionString))
                {
                    using var command = new SqlCommand("GetSupplierByIDWithFilesV2", connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@SupplierId", id);

                    await connection.OpenAsync();

                    using var reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        supplierWithFiles.Id = reader.GetInt32(reader.GetOrdinal("SupplierId"));
                        supplierWithFiles.Prefix = reader.IsDBNull(reader.GetOrdinal("Prefix")) ? string.Empty : reader.GetString(reader.GetOrdinal("Prefix"));
                        supplierWithFiles.Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name"));
                        supplierWithFiles.Tax_Id = reader.IsDBNull(reader.GetOrdinal("Tax_Id")) ? string.Empty : reader.GetString(reader.GetOrdinal("Tax_Id"));
                        supplierWithFiles.AddressSup = reader.IsDBNull(reader.GetOrdinal("AddressSup")) ? string.Empty : reader.GetString(reader.GetOrdinal("AddressSup"));
                        supplierWithFiles.District = reader.IsDBNull(reader.GetOrdinal("District")) ? string.Empty : reader.GetString(reader.GetOrdinal("District"));
                        supplierWithFiles.Subdistrict = reader.IsDBNull(reader.GetOrdinal("Subdistrict")) ? string.Empty : reader.GetString(reader.GetOrdinal("Subdistrict"));
                        supplierWithFiles.Province = reader.IsDBNull(reader.GetOrdinal("Province")) ? string.Empty : reader.GetString(reader.GetOrdinal("Province"));
                        supplierWithFiles.PostalCode = reader.IsDBNull(reader.GetOrdinal("PostalCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("PostalCode"));
                        supplierWithFiles.Tel = reader.IsDBNull(reader.GetOrdinal("Tel")) ? string.Empty : reader.GetString(reader.GetOrdinal("Tel"));
                        supplierWithFiles.Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email"));
                        supplierWithFiles.SupplierNum = reader.IsDBNull(reader.GetOrdinal("SupplierNum")) ? string.Empty : reader.GetString(reader.GetOrdinal("SupplierNum"));
                        supplierWithFiles.SupplierType = reader.IsDBNull(reader.GetOrdinal("SupplierType")) ? string.Empty : reader.GetString(reader.GetOrdinal("SupplierType"));
                        supplierWithFiles.Site = reader.IsDBNull(reader.GetOrdinal("Site")) ? string.Empty : reader.GetString(reader.GetOrdinal("Site"));
                        supplierWithFiles.Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? string.Empty : reader.GetString(reader.GetOrdinal("Status"));
                        supplierWithFiles.Vat = reader.IsDBNull(reader.GetOrdinal("Vat")) ? string.Empty : reader.GetString(reader.GetOrdinal("Vat"));
                        supplierWithFiles.PaymentMethod = reader.IsDBNull(reader.GetOrdinal("PaymentMethod")) ? string.Empty : reader.GetString(reader.GetOrdinal("PaymentMethod"));
                        supplierWithFiles.UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? 0 : reader.GetInt32(reader.GetOrdinal("UserId"));
                        supplierWithFiles.Company = reader.IsDBNull(reader.GetOrdinal("Company")) ? string.Empty : reader.GetString(reader.GetOrdinal("Company"));
                        supplierWithFiles.Type = reader.IsDBNull(reader.GetOrdinal("Type")) ? string.Empty : reader.GetString(reader.GetOrdinal("Type"));
                        supplierWithFiles.Mobile = reader.IsDBNull(reader.GetOrdinal("Mobile")) ? string.Empty : reader.GetString(reader.GetOrdinal("Mobile"));
                        supplierWithFiles.PostId = reader.IsDBNull(reader.GetOrdinal("postId")) ? 0 : reader.GetInt32(reader.GetOrdinal("postId"));
                        supplierWithFiles.Firstname = reader.IsDBNull(reader.GetOrdinal("firstname")) ? string.Empty : reader.GetString(reader.GetOrdinal("firstname"));
                        supplierWithFiles.Lastname = reader.IsDBNull(reader.GetOrdinal("lastname")) ? string.Empty : reader.GetString(reader.GetOrdinal("lastname"));
                        supplierWithFiles.EmailUser = reader.IsDBNull(reader.GetOrdinal("emailUser")) ? string.Empty : reader.GetString(reader.GetOrdinal("emailUser"));
                        supplierWithFiles.Telephone = reader.IsDBNull(reader.GetOrdinal("telephone")) ? string.Empty : reader.GetString(reader.GetOrdinal("telephone"));
                        supplierWithFiles.OwnerAcc = reader.IsDBNull(reader.GetOrdinal("OwnerAcc")) ? 0 : reader.GetInt32(reader.GetOrdinal("OwnerAcc"));
                        supplierWithFiles.OwnerFn = reader.IsDBNull(reader.GetOrdinal("OwnerFn")) ? 0 : reader.GetInt32(reader.GetOrdinal("OwnerFn"));
                    }

                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var file = new SupplierFileDto
                            {
                                FileId = reader.GetInt32(reader.GetOrdinal("FileId")),
                                SupplierId = reader.GetInt32(reader.GetOrdinal("SupplierId")),
                                FileType = reader.GetString(reader.GetOrdinal("FileType")),
                                GroupName = reader.GetString(reader.GetOrdinal("GroupName")),
                                FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                                FileName = reader.GetString(reader.GetOrdinal("FileName")),
                                LabelText = reader.GetString(reader.GetOrdinal("LabelText")),
                                UploadedDate = reader.GetDateTime(reader.GetOrdinal("UploadedDate")),
                                UploadedBy = reader.GetInt32(reader.GetOrdinal("UploadedBy"))
                            };
                            supplierWithFiles.SupplierFiles.Add(file);
                        }
                    }
                }

                if (supplierWithFiles == null)
                {
                    return NotFound();
                }

                return Ok(supplierWithFiles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("GetSupplierType")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult<IEnumerable<SupplierTypeMasterData>>> GetSupplierType()
        {
            var supplierTypes = await _appDbContext.SupplierTypeMasterData
                .FromSqlRaw("EXEC GetAllSupplierTypes")
                .ToListAsync();

            if (supplierTypes != null && supplierTypes.Count != 0)
            {
                return Ok(supplierTypes);
            }
            else
            {
                return NotFound("No supplier types found.");
            }
        }

        [HttpGet("FindSupplierTypeByID")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult<SupplierTypeMasterData>> FindSupplierTypeByID(int id)
        {
            try
            {
                var supplierType = await _supplierTypeMasterDataService.GetSupplierTypeByID(id);

                if (supplierType == null)
                {
                    return NotFound("Supplier type not found.");
                }

                return Ok(supplierType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("FindSupplierByTypeName")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
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
        [TypeFilter(typeof(CustomAuthorizationFilter))]
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
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult<IEnumerable<PaymentMethod>>> GetPaymentMethod()
        {
            return await _supplierService.GetPaymentMethodList();
        }

        [HttpGet("VatInfo")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult<IEnumerable<Vat>>> GetVatInfo()
        {
            return await _supplierService.GetVatList();
        }

        [HttpGet("CompanyInfo")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<IActionResult> CompanyInfo()
        {
            try
            {
                var companies = await _appDbContext.Company
                    .FromSqlRaw("EXEC GetAllCompanies")
                    .ToListAsync();

                if (companies != null && companies.Count != 0)
                {
                    return Ok(companies);
                }
                else
                {
                    return NotFound("No company data found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Query failed: {ex.Message}");
            }
        }

        [HttpGet("FindDataByUserID")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
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
                return StatusCode(500, $"Internal server error: {ex.Message}. StackTrace: {ex.StackTrace}");
            }
        }

        [HttpGet("GetDataByUserCompanyACC")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<IActionResult> GetDataByUserCompanyACC(string company, int userId, CancellationToken cancellationToken)
        {
            var suppliers = await _supplierService.GetDataByUserCompanyACC(company, userId, cancellationToken);
            return Ok(suppliers);
        }
        [HttpGet("GetDataByUserCompanyFN")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<IActionResult> GetDataByUserCompanyFN(string company, CancellationToken cancellationToken)
        {
            var suppliers = await _supplierService.GetDataByUserCompanyFN(company, cancellationToken);
            return Ok(suppliers);
        }

        [HttpGet("GetGroupNames")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult<List<string>>> GetGroupNames([FromQuery] string company)
        {
            try
            {
                var groupNames = await _groupService.GetGroupNamesByCompany(company);
                if (groupNames == null || groupNames.Count == 0)
                {
                    return NotFound("No groups found for the specified company.");
                }

                return Ok(groupNames);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Query failed: {ex.Message}");
            }
        }

        [HttpGet("GetAllGroups")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<IActionResult> GetAllGroups()
        {
            try
            {
                var groups = await _appDbContext.Group
                    .FromSqlRaw("EXEC GetAllGroups")
                    .ToListAsync();

                if (groups != null && groups.Count != 0)
                {
                    var groupNames = groups.Select(g => g.GroupName).ToList();
                    return Ok(groupNames);
                }
                else
                {
                    return NotFound("No group data found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Query failed: {ex.Message}");
            }
        }

        [HttpGet("CompareOracleAndSqlServerIds")]
        public async Task<IActionResult> CompareOracleAndSqlServerIds()
        {
            var oracleIds = await GetOracleIds();
            var sqlServerIds = await GetSqlServerIds();

            if (oracleIds == null || sqlServerIds == null)
            {
                return StatusCode(500, "Failed to retrieve Oracle or SQL Server IDs.");
            }

            var matchingIds = oracleIds.Intersect(sqlServerIds).ToList();

            if (matchingIds.Count != 0)
            {
                await UpdateSupplierStatusToSuccess(matchingIds);
            }

            return Ok(new
            {
                MatchingIds = matchingIds,
                TotalMatchingIds = matchingIds.Count
            });
        }

        private async Task UpdateSupplierStatusToSuccess(List<int> matchingIds)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("Id", typeof(int));

            foreach (var id in matchingIds)
            {
                dataTable.Rows.Add(id);
            }

            using var connection = new SqlConnection(_appDbContext.Database.GetConnectionString());
            using var command = new SqlCommand("UpdateSupplierStatusToSuccessX2", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            var parameter = new SqlParameter
            {
                ParameterName = "@IdList",
                SqlDbType = SqlDbType.Structured,
                TypeName = "dbo.IntList",
                Value = dataTable
            };
            command.Parameters.Add(parameter);

            await connection.OpenAsync();
            // ใช้ SqlDataReader เพื่ออ่านผลลัพธ์ที่ได้จาก Stored Procedure
            using var reader = await command.ExecuteReaderAsync();

            var results = new List<dynamic>();

            while (await reader.ReadAsync())
            {
                // เก็บข้อมูลแต่ละแถวใน List ของ object
                var result = new
                {
                    SupplierId = reader.GetInt32(reader.GetOrdinal("supplierId")),
                    SupplierNumber = reader.GetString(reader.GetOrdinal("supplierNumber")),
                    UserEmail = reader.IsDBNull(reader.GetOrdinal("userEmail")) ? null : reader.GetString(reader.GetOrdinal("userEmail")),
                    Site = reader.IsDBNull(reader.GetOrdinal("site")) ? null : reader.GetString(reader.GetOrdinal("site")),
                    OwnerAccount = reader.IsDBNull(reader.GetOrdinal("ownerAccount")) ? null : reader.GetString(reader.GetOrdinal("ownerAccount")),
                    OwnerFN = reader.IsDBNull(reader.GetOrdinal("ownerFN")) ? null : reader.GetString(reader.GetOrdinal("ownerFN")),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader.GetString(reader.GetOrdinal("Name")),
                    TaxId = reader.IsDBNull(reader.GetOrdinal("Tax_Id")) ? null : reader.GetString(reader.GetOrdinal("Tax_Id")),
                    SupplierType = reader.IsDBNull(reader.GetOrdinal("SupplierType")) ? null : reader.GetString(reader.GetOrdinal("SupplierType"))
                };

                results.Add(result);
            }
            foreach (var supplier in results)
            {
                var emailRecipients = string.Join(",", new[] { supplier.UserEmail, supplier.OwnerAccount, supplier.OwnerFN }
                    .Where(email => !string.IsNullOrWhiteSpace(email))
                    .Distinct());

                string body = "<p>เรียน ผู้เกี่ยวข้องทุกท่าน</p><br>" +
                "<p>เรื่อง : Supplier ของท่าน Sync. เข้าระบบ ERP Oracle เรียบร้อยแล้ว</p><br>" +
                "<p>คำขอ Supplier ของท่าน นำข้อมูลเข้าระบบ ERP Oracle เรียบร้อยแล้ว</p><br>" +
                "<p>Supplier Number : " + supplier.SupplierNumber + "</p>" +
                "<p>Supplier Name : " + supplier.Name + "</p>" +
                "<p>Tax ID : " + supplier.TaxId + "</p>" +
                "<p>Supplier Type : " + supplier.SupplierType + "</p><br>" +
                "<p>ท่านสามารถติดตามสถานะคำขอของท่าน ได้ที่ " +
                "<a href='http://10.10.0.28:8085/feature/supplier/view/" + supplier.SupplierId + "'>ลิงก์นี้</a></p><br>" +
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


        }
        private async Task<List<int>?> GetOracleIds()
        {
            var oracleIds = new List<int>();

            using (var connection = new OracleConnection(_oracleConnectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    using var command = new OracleCommand("GetOracleSupplierIds", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    var refCursor = command.Parameters.Add("p_cursor", OracleDbType.RefCursor);
                    refCursor.Direction = ParameterDirection.Output;

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            oracleIds.Add(reader.GetInt32(0));
                        }
                    }
                }
                catch (OracleException)
                {
                    return null;
                }
            }
            return oracleIds;
        }

        private async Task<List<int>?> GetSqlServerIds()
        {
            var sqlServerIds = new List<int>();

            using (var connection = new SqlConnection(_appDbContext.Database.GetConnectionString()))
            {
                try
                {
                    await connection.OpenAsync();

                    using var command = new SqlCommand("GetSqlServerSupplierIds", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            sqlServerIds.Add(reader.GetInt32(0));
                        }
                    }
                }
                catch (SqlException)
                {
                    return null;
                }
            }

            return sqlServerIds;
        }

        public class OracleSupplierDto
        {
            public int ID { get; set; }
            public required string SupplierNumber { get; set; }
        }

        [GeneratedRegex(@"ORA-20001:\s*(.*)", RegexOptions.Compiled)]
        private static partial Regex OracleErrorMessageRegex();

        [HttpPost("CheckDuplicateSupplier")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<IActionResult> CheckDuplicateSupplier([FromBody] SupplierCheckDto newSupplierDto)
        {
            try
            {
                var codeFrom = string.Empty;
                string sqlServerMessage = "No duplicate supplier found.";

                using (var sqlConnection = new SqlConnection(_appDbContext.Database.GetConnectionString()))
                {
                    using var sqlCommand = new SqlCommand("GetSupplierCodeFromType", sqlConnection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    sqlCommand.Parameters.AddWithValue("@SupplierType", newSupplierDto.SupplierType);
                    var codeFromOutput = new SqlParameter("@CodeFrom", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Output };
                    sqlCommand.Parameters.Add(codeFromOutput);

                    await sqlConnection.OpenAsync();
                    await sqlCommand.ExecuteNonQueryAsync();
                    codeFrom = codeFromOutput.Value?.ToString();

                    if (string.IsNullOrEmpty(codeFrom))
                    {
                        return BadRequest("ไม่พบ code_from สำหรับ SupplierType ที่ระบุ");
                    }
                }

                try
                {
                    using var oracleConnection = new OracleConnection(_oracleConnectionString);
                    using var oracleCommand = new OracleCommand("CHECK_DUPLICATE_SUPPLIER_ORACLE", oracleConnection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    oracleCommand.Parameters.Add("p_tax", OracleDbType.Varchar2).Value = newSupplierDto.TaxId;
                    oracleCommand.Parameters.Add("p_code_from", OracleDbType.Varchar2).Value = codeFrom;
                    oracleCommand.Parameters.Add("p_name", OracleDbType.Varchar2).Value = newSupplierDto.name;
                    var pCount = new OracleParameter("p_count", OracleDbType.Int32) { Direction = ParameterDirection.Output };
                    oracleCommand.Parameters.Add(pCount);

                    await oracleConnection.OpenAsync();
                    await oracleCommand.ExecuteNonQueryAsync();
                }
                catch (OracleException ex) when (ex.Number == 20001)
                {
                    var match = OracleErrorMessageRegex().Match(ex.Message);
                    var customMessage = match.Success ? match.Groups[1].Value.Trim() : "An error occurred in Oracle.";
                    return StatusCode(409, $"{customMessage}");
                }

                using (var sqlConnection = new SqlConnection(_appDbContext.Database.GetConnectionString()))
                {
                    using var sqlCommand = new SqlCommand("CheckDuplicateSupplierX2", sqlConnection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    sqlCommand.Parameters.AddWithValue("@TaxId", newSupplierDto.TaxId);
                    sqlCommand.Parameters.AddWithValue("@SupplierType", newSupplierDto.SupplierType);
                    sqlCommand.Parameters.AddWithValue("@Name", newSupplierDto.name);

                    await sqlConnection.OpenAsync();
                    try
                    {
                        await sqlCommand.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex) when (ex.Number == 50000)
                    {
                        sqlServerMessage = ex.Message;
                        return Conflict(sqlServerMessage);
                    }
                }

                return Ok(sqlServerMessage);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("GetSupplierFileTemplates")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult<IEnumerable<SupplierFileTemplateDto>>> GetSupplierFileTemplates()
        {
            try
            {
                List<SupplierFileTemplateDto> templates;

                using (var connection = new SqlConnection(_appDbContext.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    templates = (await connection.QueryAsync<SupplierFileTemplateDto>("EXEC GetSupplierFileTemplates")).ToList();
                }

                if (templates == null || templates.Count == 0)
                {
                    return NotFound(new { message = "No templates found." });
                }

                return Ok(templates);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving templates.", details = ex.Message });
            }
        }

        

        [HttpPost("AddOrUpdateSupplierWithBankAndFiles")]
        [RequestSizeLimit(100_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<ActionResult> AddOrUpdateSupplierWithBankAndFiles(
        [FromForm] string supplierJson,
        [FromForm] string supplierBankJson,
        [FromForm] List<IFormFile> SupplierFiles,
        [FromForm] List<IFormFile> SupplierBankFiles,
        [FromForm] string SupplierFilesMetadata,
        [FromForm] string SupplierBankFilesMetadata,
        [FromForm] string? fileIdsToRemoveJson = null,
        [FromForm] string? fileBankIdsToRemoveJson = null,
        [FromForm] string? rejectReason = null)
        {
            SupplierDto? supplier;
            List<SupplierBankDto> supplierBanks;
            List<SupplierBankFileRemovalDto>? fileIdsToRemove = null;
            List<SupplierBankFileRemovalDto>? fileBankIdsToRemove = null;
            List<SupplierFileMetadata> supplierFilesMetadata;
            List<SupplierBankFileMetadata> supplierBankFilesMetadata;

            try
            {
                supplier = JsonConvert.DeserializeObject<SupplierDto>(supplierJson);
                supplierBanks = JsonConvert.DeserializeObject<List<SupplierBankDto>>(supplierBankJson) ?? [];
                supplierFilesMetadata = JsonConvert.DeserializeObject<List<SupplierFileMetadata>>(SupplierFilesMetadata) ?? [];
                supplierBankFilesMetadata = JsonConvert.DeserializeObject<List<SupplierBankFileMetadata>>(SupplierBankFilesMetadata) ?? [];

                if (!string.IsNullOrWhiteSpace(fileIdsToRemoveJson))
                {
                    var fileIdList = JsonConvert.DeserializeObject<List<int>>(fileIdsToRemoveJson) ?? [];
                    fileIdsToRemove = fileIdList.Select(id => new SupplierBankFileRemovalDto { FileId = id }).ToList();
                }

                if (!string.IsNullOrWhiteSpace(fileBankIdsToRemoveJson))
                {
                    fileBankIdsToRemove = JsonConvert.DeserializeObject<List<SupplierBankFileRemovalDto>>(fileBankIdsToRemoveJson) ?? [];
                }

                if (supplier == null || supplierBanks == null)
                {
                    return BadRequest(new { message = "Invalid supplier data or supplier bank data." });
                }
            }
            catch (JsonSerializationException ex)
            {
                return BadRequest(new { message = "Invalid JSON format.", details = ex.Message });
            }

            var filesTable = CreateFilesTable();
            var supplierBankFilesTable = await CreateSupplierBankFilesTable(SupplierBankFiles, supplierBankFilesMetadata, supplier.UserId);
            var supplierBankTable = CreateSupplierBankTable(supplierBanks);
            var fileIdsToRemoveTable = CreateFileIdsToRemoveTable(fileIdsToRemove);
            var fileBankIdsToRemoveTable = CreateFileIdsToRemoveTable(fileBankIdsToRemove);
            await DeleteFilesFromDb(fileIdsToRemove, "dbo.INTTableType");

            if (fileBankIdsToRemove != null && fileBankIdsToRemove.Count > 0)
            {
                var filePathsToRemove = fileBankIdsToRemove
                    .Select(fileRemoval => GetFilePathById(fileRemoval.FileId))
                    .Where(filePath => !string.IsNullOrEmpty(filePath))
                    .ToList();

                DeleteFiles(filePathsToRemove);
            }

            await UploadFiles(SupplierFiles, supplierFilesMetadata, filesTable, supplier.UserId, "suppliers");

            if (supplier.Status.Equals("Approved By ACC"))
            {
                using var connection = new SqlConnection(_appDbContext.Database.GetConnectionString());
                await connection.OpenAsync();
                using var command = new SqlCommand("GetNextSupplierNumByTypeForInterface", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@SupplierType", supplier.SupplierType ?? (object)DBNull.Value);
                var outputParam = new SqlParameter("@SupplierNum", SqlDbType.NVarChar, 50)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(outputParam);

                // เรียก Stored Procedure
                await command.ExecuteNonQueryAsync();

                // รับค่าจากพารามิเตอร์ Output
                var supplierNum = outputParam.Value?.ToString();
                supplier.SupplierNum = supplierNum;

                var emptySupplierBank = new SupplierBankDto
                {
                    SupbankId = 0,
                    SupplierId = 0,
                    NameBank = string.Empty,
                    Branch = string.Empty,
                    AccountNum = string.Empty,
                    AccountName = string.Empty,
                    SupplierGroup = string.Empty,
                    Company = string.Empty,
                    LabelTextsV2 = []
                };
                //await SyncDataToOracle(supplier, emptySupplierBank);

            }

            if (supplier.Status.Equals("Approved By FN") && supplier.PaymentMethod.Equals("Transfer"))
            {
                foreach (var supplierBank in supplierBanks)
                {
                    //await SyncDataToOracle(supplier, supplierBank);
                }
            }

            try
            {
                using var connection = new SqlConnection(_appDbContext.Database.GetConnectionString());
                await connection.OpenAsync();
                using var command = new SqlCommand("AddOrUpdateSupplierAndBankWithFilesV3", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@SupplierId", supplier.Id ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Prefix", supplier.Prefix ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Name", supplier.Name ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TaxId", supplier.Tax_Id ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AddressSup", supplier.AddressSup ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@District", supplier.District ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Subdistrict", supplier.Subdistrict ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Province", supplier.Province ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PostalCode", supplier.PostalCode ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Tel", supplier.Tel ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Email", supplier.Email ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SupplierType", supplier.SupplierType ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Site", supplier.Site ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", supplier.Status ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Vat", supplier.Vat ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PaymentMethod", supplier.PaymentMethod ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@UserId", supplier.UserId);
                command.Parameters.AddWithValue("@Company", supplier.Company ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Type", supplier.Type ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Mobile", supplier.Mobile ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@OwnerAcc", supplier.OwnerAcc ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@OwnerFN", supplier.OwnerFn ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PostId", supplier.PostId ?? (object)DBNull.Value);
                if (!string.IsNullOrEmpty(rejectReason) && rejectReason?.StartsWith('"') == true && rejectReason?.EndsWith('"') == true)
                {
                    rejectReason = rejectReason[1..^1];
                }

                rejectReason = rejectReason?.Replace("\\\"", "\"");

                command.Parameters.AddWithValue("@RejectReason", rejectReason ?? (object)DBNull.Value);

                command.Parameters.Add(new SqlParameter("@Files", SqlDbType.Structured)
                {
                    TypeName = "dbo.FileTableType",
                    Value = filesTable
                });

                command.Parameters.Add(new SqlParameter("@SupplierBankFiles", SqlDbType.Structured)
                {
                    TypeName = "dbo.SupplierBankFileTableType",
                    Value = supplierBankFilesTable
                });

                command.Parameters.Add(new SqlParameter("@SupplierBanks", SqlDbType.Structured)
                {
                    TypeName = "dbo.SupplierBankTableType",
                    Value = supplierBankTable
                });

                command.Parameters.Add(new SqlParameter("@FileIdsToRemove", SqlDbType.Structured)
                {
                    TypeName = "dbo.SupplierBankFileRemovalTableType",
                    Value = fileIdsToRemoveTable
                });

                command.Parameters.Add(new SqlParameter("@FileBankIdsToRemove", SqlDbType.Structured)
                {
                    TypeName = "dbo.SupplierBankFileRemovalTableType",
                    Value = fileBankIdsToRemoveTable
                });

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the database.", details = ex.Message });
            }

            return Ok(new { message = "Supplier and bank details with files added/updated successfully." });
        }

        //private async Task ProcessFilesWithWatermark<T>(
        //List<IFormFile> files,
        //List<T> metadata,
        //DataTable filesTable,
        //int userId,
        //string folder) where T : class
        //{
        //    if (files == null || files.Count == 0) return;

        //    for (int i = 0; i < files.Count; i++)
        //    {
        //        var file = files[i];
        //        if (file.Length > 0)
        //        {
        //            var fileExtension = Path.GetExtension(file.FileName).ToLower();
        //            string outputFilePath;
        //            if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png")
        //            {
        //                // 🖼 เพิ่มลายน้ำรูปภาพ
        //                outputFilePath = await _watermarkService.AddWatermarkToImage(file, "Confidential", "bottom_right", 150);
        //            }
        //            else if (fileExtension == ".pdf")
        //            {
        //                // 📄 เพิ่มลายน้ำ PDF
        //                outputFilePath = await _watermarkService.AddWatermarkToPdf(file, "Confidential", "center", 100);
        //            }
        //            else
        //            {
        //                // 🔴 ถ้าเป็นไฟล์อื่นๆ ให้บันทึกตามปกติ
        //                var uploadsFolder = Path.Combine("uploads", folder);
        //                if (!Directory.Exists(uploadsFolder))
        //                {
        //                    Directory.CreateDirectory(uploadsFolder);
        //                }

        //                outputFilePath = Path.Combine(uploadsFolder, $"{Guid.NewGuid()}_{file.FileName}");
        //                using var fileStream = new FileStream(outputFilePath, FileMode.Create);
        //                await file.CopyToAsync(fileStream);
        //            }

        //            // ✅ รองรับทั้ง SupplierFileMetadata และ SupplierBankFileMetadata
        //            string labelText = "";
        //            if (metadata.Count > i)
        //            {
        //                var meta = metadata[i];
        //                var labelTextProp = meta.GetType().GetProperty("LabelText");
        //                if (labelTextProp != null)
        //                {
        //                    labelText = labelTextProp.GetValue(meta)?.ToString() ?? "";
        //                }
        //            }

        //            filesTable.Rows.Add(
        //                file.ContentType,
        //                folder,
        //                outputFilePath,
        //                file.FileName,
        //                labelText,
        //                DateTime.Now,
        //                userId
        //            );
        //        }
        //    }
        //}

        private string GetFilePathById(int fileId)
        {
            using var connection = new SqlConnection(_appDbContext.Database.GetConnectionString());
            const string query = "SELECT FilePath FROM SupplierBankFiles WHERE Id = @FileId";

            try
            {
                connection.Open();
                var filePath = connection.QueryFirstOrDefault<string>(query, new { FileId = fileId });
                return filePath ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving file path for FileId {fileId}: {ex.Message}");
                return string.Empty;
            }
        }

        private static DataTable CreateFileIdsToRemoveTable(List<SupplierBankFileRemovalDto>? fileIdsToRemove)
        {
            var table = new DataTable();
            table.Columns.Add("SupbankId", typeof(int));
            table.Columns.Add("FileId", typeof(int));

            if (fileIdsToRemove != null)
            {
                foreach (var removal in fileIdsToRemove)
                {
                    table.Rows.Add(removal.SupbankId, removal.FileId);
                }
            }

            return table;
        }

        private static void DeleteFiles(List<string> filesToRemove)
        {
            if (filesToRemove == null || filesToRemove.Count == 0) return;

            foreach (var filePath in filesToRemove)
            {
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting file at {filePath}: {ex.Message}");
                    }
                }
            }
        }

        private static DataTable CreateFilesTable()
        {
            var table = new DataTable();
            table.Columns.Add("FileType", typeof(string));
            table.Columns.Add("GroupName", typeof(string));
            table.Columns.Add("FilePath", typeof(string));
            table.Columns.Add("FileName", typeof(string));
            table.Columns.Add("LabelText", typeof(string));
            table.Columns.Add("UploadedDate", typeof(DateTime));
            table.Columns.Add("UploadedBy", typeof(int));

            return table;
        }

        private  async Task<DataTable> CreateSupplierBankFilesTable(
        List<IFormFile> supplierBankFiles,
        List<SupplierBankFileMetadata> supplierBankFilesMetadata,
        int userId)
        {
            var table = new DataTable();
            table.Columns.Add("FileType", typeof(string));
            table.Columns.Add("GroupName", typeof(string));
            table.Columns.Add("FilePath", typeof(string));
            table.Columns.Add("FileName", typeof(string));
            table.Columns.Add("LabelText", typeof(string));
            table.Columns.Add("UploadedDate", typeof(DateTime));
            table.Columns.Add("UploadedBy", typeof(int));

            for (int i = 0; i < supplierBankFiles.Count; i++)
            {
                var file = supplierBankFiles[i];
                var metadata = i < supplierBankFilesMetadata.Count ? supplierBankFilesMetadata[i] : null;
                string groupName = metadata?.SupplierGroup ?? "Unknown";
                string labelText = metadata?.LabelText ?? "Label";

                var uploadsFolderPath = Path.Combine("wwwroot/uploads", groupName);
                if (!Directory.Exists(uploadsFolderPath))
                {
                    Directory.CreateDirectory(uploadsFolderPath);
                }

                var id = Guid.NewGuid();
                var originalFileName = id + "_" + file.FileName;
                var filePath = Path.Combine(uploadsFolderPath, originalFileName);
                var uploadPathFix = Path.Combine("uploads", groupName, originalFileName); // ✅ Path ที่บันทึกในฐานข้อมูล

                var fileExtension = Path.GetExtension(file.FileName).ToLower();

                try
                {
                    if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png")
                    {
                        //uploadPathFix = await _watermarkService.AddWatermarkToImage(file, "Confidential", "bottom_right", 150, groupName);
                    }
                    else if (fileExtension == ".pdf")
                    {
                        uploadPathFix = await _watermarkService.AddWatermarkToPdfAspose(file, "Confidential", groupName);
                        //uploadPathFix = await _watermarkService.AddWatermarkToPdf(file, "Confidential", "center", 100, groupName);
                    }
                    else
                    {
                        // 🟢 อัปโหลดไฟล์ประเภทอื่นๆ ปกติ
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        uploadPathFix = Path.Combine("uploads", groupName, Path.GetFileName(filePath));
                    }

                    // ✅ Log การอัปโหลดเพื่อตรวจสอบ
                    Console.WriteLine($"File Uploaded: {uploadPathFix}");

                    table.Rows.Add(
                        file.ContentType,
                        groupName,
                        uploadPathFix,
                        file.FileName,
                        labelText,
                        DateTime.Now,
                        userId
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error uploading file {file.FileName}: {ex.Message}");
                    throw; // ให้เกิด error เพื่อ debug ได้ง่ายขึ้น
                }
            }

            return table;
        }


        private static DataTable CreateSupplierBankTable(List<SupplierBankDto> supplierBanks)
        {
            var table = new DataTable();
            table.Columns.Add("SupplierId", typeof(int));
            table.Columns.Add("NameBank", typeof(string));
            table.Columns.Add("Branch", typeof(string));
            table.Columns.Add("AccountNum", typeof(string));
            table.Columns.Add("AccountName", typeof(string));
            table.Columns.Add("SupplierGroup", typeof(string));
            table.Columns.Add("Company", typeof(string));

            foreach (var bank in supplierBanks)
            {
                table.Rows.Add(
                    (object)bank.SupplierId,
                    !string.IsNullOrEmpty(bank.NameBank) ? (object)bank.NameBank : DBNull.Value,
                    !string.IsNullOrEmpty(bank.Branch) ? (object)bank.Branch : DBNull.Value,
                    !string.IsNullOrEmpty(bank.AccountNum) ? (object)bank.AccountNum : DBNull.Value,
                    !string.IsNullOrEmpty(bank.AccountName) ? (object)bank.AccountName : DBNull.Value,
                    !string.IsNullOrEmpty(bank.SupplierGroup) ? (object)bank.SupplierGroup : DBNull.Value,
                    !string.IsNullOrEmpty(bank.Company) ? (object)bank.Company : DBNull.Value
                );
            }

            return table;
        }

        private  async Task UploadFiles(List<IFormFile> files, List<SupplierFileMetadata> metadata, DataTable table, int userId, string folderName)
        {
            var uploadPath = Path.Combine("wwwroot/uploads", folderName);

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                if (file.Length > 0)
                {
                    var id = Guid.NewGuid();
                    var originalFileName = id + "_" + file.FileName;
                    var filePath = Path.Combine(uploadPath, originalFileName);
                    var uploadPathFix = Path.Combine("uploads", folderName, originalFileName);

                    // 🟢 ตรวจสอบว่าเป็นไฟล์ที่ต้องเพิ่มลายน้ำหรือไม่
                    var fileExtension = Path.GetExtension(file.FileName).ToLower();
                    if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png")
                    {
                        //filePath = await _watermarkService.AddWatermarkToImage(file, "Confidential", "center", 20, folderName);
                        uploadPathFix = Path.Combine("uploads", folderName, Path.GetFileName(filePath)); // ตัด wwwroot ออก
                    }
                    else if (fileExtension == ".pdf")
                    {
                        filePath = await _watermarkService.AddWatermarkToPdfAspose(file, "Confidential", folderName);
                        uploadPathFix = Path.Combine("uploads", folderName, Path.GetFileName(filePath)); // ตัด wwwroot ออก
                    }
                    else
                    {
                        using var stream = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(stream);
                    }

                    var fileMeta = i < metadata.Count ? metadata[i] : null;
                    table.Rows.Add(
                        file.ContentType,
                        fileMeta?.SupplierGroup ?? "Unknown",
                        uploadPathFix,  // ✅ บันทึกพาธที่ถูกต้องลง DB
                        file.FileName,
                        fileMeta?.LabelText ?? "Label",
                        DateTime.Now,
                        userId
                    );
                }
            }
        }


        private async Task DeleteFilesFromDb(List<SupplierBankFileRemovalDto>? fileIdsToRemove, string tableTypeName)
        {
            if (fileIdsToRemove != null && fileIdsToRemove.Count > 0)
            {
                var fileIdList = fileIdsToRemove.Select(dto => dto.FileId).ToList();
                using var connection = new SqlConnection(_appDbContext.Database.GetConnectionString());
                using var deleteCommand = new SqlCommand("GetFilePathsFromSupplierFiles", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                var fileIdsToRemoveParameter = new SqlParameter("@FileIds", SqlDbType.Structured)
                {
                    TypeName = tableTypeName,
                    Value = CreateIntDataTable(fileIdList)
                };
                deleteCommand.Parameters.Add(fileIdsToRemoveParameter);

                await connection.OpenAsync();
                using var reader = await deleteCommand.ExecuteReaderAsync();
                var filesToRemove = new List<string>();
                while (await reader.ReadAsync()) filesToRemove.Add(reader.GetString(0));

                foreach (var filePath in filesToRemove)
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            }
        }

        private static DataTable CreateSupplierBankDataTable(List<SupplierBankWithFilesDto> supplierBanks)
        {
            var table = new DataTable();
            table.Columns.Add("SupplierId", typeof(int));
            table.Columns.Add("NameBank", typeof(string));
            table.Columns.Add("Branch", typeof(string));
            table.Columns.Add("AccountNum", typeof(string));
            table.Columns.Add("AccountName", typeof(string));
            table.Columns.Add("SupplierGroup", typeof(string));
            table.Columns.Add("Company", typeof(string));

            foreach (var bank in supplierBanks)
            {
                table.Rows.Add(bank.SupplierId, bank.NameBank, bank.Branch, bank.AccountNum,
                               bank.AccountName, bank.SupplierGroup, bank.Company);
            }

            return table;
        }

        private static DataTable CreateFileIdsToRemoveDataTable(List<SupplierBankFileRemovalDto> fileIdsToRemove)
        {
            var table = new DataTable();
            table.Columns.Add("SupbankId", typeof(int));
            table.Columns.Add("FileId", typeof(int));

            foreach (var removal in fileIdsToRemove)
            {
                table.Rows.Add(removal.SupbankId, removal.FileId);
            }

            return table;
        }

        [HttpPost("ExportApprovedSuppliersToExcelDynamic")]
        [TypeFilter(typeof(CustomAuthorizationFilter))]
        public async Task<IActionResult> ExportApprovedSuppliersToExcelDynamic([FromBody] JsonElement requestBody)
        {
            try
            {
                if (!requestBody.TryGetProperty("Username", out var usernameElement) 
                    || !requestBody.TryGetProperty("StartDate", out var startDateElement)
                    || !requestBody.TryGetProperty("EndDate", out var endDateElement)
                    )
                {
                    return BadRequest("Invalid request body. Required properties: Username, Date.");
                }

                string username = usernameElement.GetString() ?? string.Empty;
                DateTime startDate = startDateElement.GetDateTime();
                DateTime endDate = endDateElement.GetDateTime();
                string dateRange = $"{startDate:dd-MM-yyyy} ถึง {endDate:dd-MM-yyyy}";

                if (string.IsNullOrEmpty(username))
                {
                    return BadRequest("Username is required.");
                }
                if (startDate > endDate)
                {
                    return BadRequest("StartDate cannot be later than EndDate.");
                }

                var connectionString = _appDbContext.Database.GetConnectionString();

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using (var logCommand = new SqlCommand("sp_InsertExportLog", connection))
                {
                    logCommand.CommandType = CommandType.StoredProcedure;
                    logCommand.Parameters.AddWithValue("@Username", username);
                    logCommand.Parameters.AddWithValue("@DateRange", dateRange);
                    await logCommand.ExecuteNonQueryAsync(); 
                }

                using var command = new SqlCommand("GetApprovedSuppliersByDateX2", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@StartDate ", startDate);
                command.Parameters.AddWithValue("@EndDate ", endDate);

                using var reader = await command.ExecuteReaderAsync();

                var dataTable = new DataTable();
                dataTable.Load(reader);

                if (dataTable.Rows.Count == 0)
                {
                    return NotFound("No approved suppliers found.");
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Approved Suppliers");

                var columnNames = dataTable.Columns.Cast<DataColumn>().Select(col => col.ColumnName).ToList();
                for (int i = 0; i < columnNames.Count; i++)
                {
                    worksheet.Cells[1, i + 1].Value = columnNames[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                }

                for (int row = 0; row < dataTable.Rows.Count; row++)
                {
                    for (int col = 0; col < columnNames.Count; col++)
                    {
                        worksheet.Cells[row + 2, col + 1].Value = dataTable.Rows[row][col];
                    }
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"ApprovedSuppliers_{username}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while generating the Excel file: {ex.Message}");
            }
        }

        private async Task<IActionResult> SyncDataToOracle(SupplierDto supplier, SupplierBankDto supplierBank)
        {
            try
            {
                
                    using var oracleConnection = new OracleConnection(_oracleConnectionString);
                await oracleConnection.OpenAsync();
                using var oracleCommand = new OracleCommand("INSERT_SUPPLIER_SYNC", oracleConnection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                oracleCommand.Parameters.Add("p_Id", OracleDbType.Int32).Value = supplier.Id ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_prefix", OracleDbType.Varchar2).Value = supplier.Prefix ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_Name", OracleDbType.Varchar2).Value = supplier.Name ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_Tax_Id", OracleDbType.Varchar2).Value = supplier.Tax_Id ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_AddressSup", OracleDbType.Varchar2).Value = supplier.AddressSup ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_district", OracleDbType.Varchar2).Value = supplier.District ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_subdistrict", OracleDbType.Varchar2).Value = supplier.Subdistrict ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_province", OracleDbType.Varchar2).Value = supplier.Province ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_postalCode", OracleDbType.Varchar2).Value = supplier.PostalCode ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_tel", OracleDbType.Varchar2).Value = supplier.Tel ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_email", OracleDbType.Varchar2).Value = supplier.Email ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_SupplierNum", OracleDbType.Varchar2).Value = supplier.SupplierNum ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_SupplierType", OracleDbType.Varchar2).Value = supplier.SupplierType ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_site", OracleDbType.Varchar2).Value = supplier.Site ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_status", OracleDbType.Varchar2).Value = supplier.Status ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_vat", OracleDbType.Varchar2).Value = supplier.Vat ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_PaymentMethod", OracleDbType.Varchar2).Value = supplier.PaymentMethod ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_UserId", OracleDbType.Int32).Value = supplier.UserId;
                oracleCommand.Parameters.Add("p_company", OracleDbType.Varchar2).Value = supplier.Company ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_type", OracleDbType.Varchar2).Value = supplier.Type ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_OwnerAcc", OracleDbType.Varchar2).Value = supplier.OwnerAcc ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_OwnerFn", OracleDbType.Varchar2).Value = supplier.OwnerFn ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_mobile", OracleDbType.Varchar2).Value = supplier.Mobile ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_postId", OracleDbType.Int32).Value = supplier.PostId ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_SupbankId", OracleDbType.Int32).Value = supplierBank?.SupbankId ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_SupplierId", OracleDbType.Int32).Value = supplierBank?.SupplierId ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_NameBank", OracleDbType.Varchar2).Value = supplierBank?.NameBank ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_branch", OracleDbType.Varchar2).Value = supplierBank?.Branch ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_AccountNum", OracleDbType.Varchar2).Value = supplierBank?.AccountNum ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_AccountName", OracleDbType.Varchar2).Value = supplierBank?.AccountName ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_SupplierGroup", OracleDbType.Varchar2).Value = supplierBank?.SupplierGroup ?? (object)DBNull.Value;
                oracleCommand.Parameters.Add("p_company_2", OracleDbType.Varchar2).Value = supplierBank?.Company ?? (object)DBNull.Value;


                await oracleCommand.ExecuteNonQueryAsync();
                return Ok(new { message = "Data synchronized successfully." });
            }
            catch (OracleException ex) when (ex.Number == 20001)
            {
                var match = OracleErrorMessageRegex().Match(ex.Message);
                var customMessage = match.Success ? match.Groups[1].Value.Trim() : "An error occurred in Oracle.";
                return StatusCode(409, new { message = customMessage });
            }
        }
    }
}