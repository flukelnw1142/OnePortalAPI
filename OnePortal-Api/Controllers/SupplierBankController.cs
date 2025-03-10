using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OnePortal_Api.Data;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;
using OnePortal_Api.Services;
using System.Data;
using Dapper;
using System.Linq;
using OnePortal_Api.Filters;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [TypeFilter(typeof(CustomAuthorizationFilter))]
    public class SupplierBankController(ISupplierBankService supplierBankService, AppDbContext appDbContext) : Controller
    {
        private const string V = "UpdateSupplierBankWithFiles/{id}";
        private readonly ISupplierBankService _supplierBankService = supplierBankService;
        private readonly AppDbContext _appDbContext = appDbContext;

        [HttpPost("SaveSupplierBanksWithFiles")]
        [RequestSizeLimit(100_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)]
        public async Task<ActionResult> SaveSupplierBanksWithFiles(
        [FromForm] string supplierBankJson,
        [FromForm] List<IFormFile> Files,
        [FromForm] string? fileIdsToRemoveJson = null,
        [FromForm] string? labelTextsJson = null)
        {
            const long maxFileSize = 100 * 1024 * 1024;

            foreach (var file in Files)
            {
                if (file.Length > maxFileSize)
                {
                    return BadRequest(new { message = $"File '{file.FileName}' exceeds the maximum allowed size of {maxFileSize / (1024 * 1024)} MB." });
                }
            }

            List<SupplierBankWithFilesDto>? supplierBanks;
            List<SupplierBankFileRemovalDto>? fileIdsToRemove = null;
            Dictionary<string, List<string>>? labelTexts = null;

            try
            {
                if (string.IsNullOrWhiteSpace(supplierBankJson))
                {
                    return BadRequest(new { message = "No JSON data received." });
                }

                supplierBanks = JsonConvert.DeserializeObject<List<SupplierBankWithFilesDto>>(supplierBankJson);

                if (!string.IsNullOrEmpty(fileIdsToRemoveJson))
                {
                    fileIdsToRemove = JsonConvert.DeserializeObject<List<SupplierBankFileRemovalDto>>(fileIdsToRemoveJson);
                }

                if (!string.IsNullOrEmpty(labelTextsJson))
                {
                    labelTexts = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(labelTextsJson);
                }

                if (supplierBanks == null || supplierBanks.Count == 0)
                {
                    return BadRequest(new { message = "No valid supplier bank data found in JSON." });
                }
            }
            catch (JsonSerializationException ex)
            {
                return BadRequest(new { message = "Invalid JSON format.", details = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "An error occurred while processing the data.", details = ex.Message });
            }

            Console.WriteLine("Files Count: " + Files.Count);
            Files.ForEach(f => Console.WriteLine($"File: {f.FileName}, Size: {f.Length}"));

            Console.WriteLine("Supplier Banks Count: " + supplierBanks.Count);
            supplierBanks.ForEach(sb => Console.WriteLine($"SupplierId: {sb.SupplierId}, NameBank: {sb.NameBank}"));

            Console.WriteLine("File IDs to Remove Count: " + (fileIdsToRemove?.Count ?? 0));
            fileIdsToRemove?.ForEach(fr => Console.WriteLine($"FileId: {fr.FileId}, SupbankId: {fr.SupbankId}"));

            Console.WriteLine("Label Texts Count: " + (labelTexts?.Count ?? 0));
            if (labelTexts != null)
            {
                foreach (var labelText in labelTexts)
                {
                    Console.WriteLine($"GroupName: {labelText.Key}");
                    labelText.Value.ForEach(lt => Console.WriteLine($"Label: {lt}"));
                }
            }

            if (fileIdsToRemove != null && fileIdsToRemove.Count > 0)
            {
                var filePathsToRemove = fileIdsToRemove
                    .Select(fileRemoval => GetFilePathById(fileRemoval.FileId))
                    .Where(filePath => !string.IsNullOrEmpty(filePath))
                    .ToList();

                DeleteFiles(filePathsToRemove);
            }

            var supplierBankTable = CreateSupplierBankDataTable(supplierBanks);
            var filesTable = CreateFilesDataTable(Files, supplierBanks, fileIdsToRemove ?? [], labelTexts ?? []);
            var fileIdsToRemoveTable = fileIdsToRemove != null ? CreateFileIdsToRemoveDataTable(fileIdsToRemove) : new DataTable();

            using var connection = new SqlConnection(_appDbContext.Database.GetConnectionString());
            using var command = new SqlCommand("AddOrUpdateSupplierBankWithFilesV5", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@SupplierBanks", SqlDbType.Structured)
            {
                TypeName = "dbo.SupplierBankTableType",
                Value = supplierBankTable
            });

            command.Parameters.Add(new SqlParameter("@Files", SqlDbType.Structured)
            {
                TypeName = "dbo.FileTableType",
                Value = filesTable
            });

            if (fileIdsToRemove != null)
            {
                command.Parameters.Add(new SqlParameter("@FileIdsToRemove", SqlDbType.Structured)
                {
                    TypeName = "dbo.SupplierBankFileRemovalTableType",
                    Value = fileIdsToRemoveTable
                });
            }

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            return Ok(new { message = "Supplier banks and files saved successfully." });
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

        private DataTable CreateFilesDataTable(List<IFormFile> files, List<SupplierBankWithFilesDto> supplierBanks, List<SupplierBankFileRemovalDto> fileIdsToRemove, Dictionary<string, List<string>> labelTexts)
        {
            var table = new DataTable();
            table.Columns.Add("FileType", typeof(string));
            table.Columns.Add("GroupName", typeof(string));
            table.Columns.Add("FilePath", typeof(string));
            table.Columns.Add("FileName", typeof(string));
            table.Columns.Add("LabelText", typeof(string));
            table.Columns.Add("UploadedDate", typeof(DateTime));
            table.Columns.Add("UploadedBy", typeof(int));

            Console.WriteLine("Starting CreateFilesDataTable process...");
            Console.WriteLine("Total Files Count: " + files.Count);
            Console.WriteLine("Supplier Banks Count: " + supplierBanks.Count);
            Console.WriteLine("File IDs to Remove Count: " + (fileIdsToRemove?.Count ?? 0));
            Console.WriteLine("Label Texts Count: " + (labelTexts?.Count ?? 0));

            var labelEntries = (labelTexts ?? [])
                .SelectMany(entry => entry.Value.Select(label => new { GroupName = entry.Key.Replace(" ", "").ToUpper(), Label = label }))
                .ToList();

            Console.WriteLine("Label Entries Flattened Count: " + labelEntries.Count);

            var templates = GetSupplierBankFileTemplates();
            int labelIndex = 0;

            foreach (var bank in supplierBanks)
            {
                var supbankId = bank.SupbankId ?? 0;
                var supplierGroup = (bank.SupplierGroup ?? "default").Replace(" ", "").ToUpper();

                Console.WriteLine($"Processing SupplierBank with SupbankId: {supbankId}, SupplierGroup: {supplierGroup}");

                var relevantTemplates = templates
                    .Where(template => template.GroupName.Replace(" ", "").ToUpper().Equals(supplierGroup))
                    .ToList();

                Console.WriteLine($"Relevant Templates Count for SupplierGroup '{supplierGroup}': {relevantTemplates.Count}");

                var relevantLabels = labelEntries
                    .Where(entry => relevantTemplates.Any(template => template.LabelText.Equals(entry.Label, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                Console.WriteLine($"Relevant Labels Count for SupplierGroup '{supplierGroup}': {relevantLabels.Count}");

                bool isNewUploadOnly = fileIdsToRemove == null || fileIdsToRemove.Count == 0;
                Console.WriteLine($"Is New Upload Only: {isNewUploadOnly}");

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
                            Console.WriteLine($"Created directory: {uploadsFolder}");
                        }

                        var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        Console.WriteLine($"Saving File: {file.FileName}, Path: {filePath}");

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            file.CopyTo(stream);
                        }

                        var fileType = file.ContentType;
                        var labelText = labelEntry.Label;

                        Console.WriteLine($"Adding File Row - Type: {fileType}, Group: {bank.SupplierGroup}, Name: {file.FileName}, Label: {labelText}");

                        table.Rows.Add(fileType, bank.SupplierGroup, filePath, file.FileName, labelText, DateTime.Now, bank.UploadedBy);
                        labelIndex++;
                    }
                }
                else
                {
                    var filesToProcess = (fileIdsToRemove ?? Enumerable.Empty<SupplierBankFileRemovalDto>())
                        .Where(removal => removal.SupbankId == supbankId)
                        .ToList();

                    Console.WriteLine($"Files to Process for Update (with fileIdsToRemove) Count: {filesToProcess.Count}");

                    foreach (var fileRemoval in filesToProcess)
                    {
                        var file = files.ElementAtOrDefault(labelIndex);
                        if (file == null) continue;

                        var uploadsFolder = Path.Combine("uploads", bank.SupplierGroup ?? "default");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                            Console.WriteLine($"Created directory: {uploadsFolder}");
                        }

                        var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        Console.WriteLine($"Updating File: {file.FileName}, Path: {filePath}");

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            file.CopyTo(stream);
                        }

                        var fileType = file.ContentType;
                        var labelText = labelIndex < relevantLabels.Count ? relevantLabels[labelIndex].Label : "No Label";

                        Console.WriteLine($"Adding Updated File Row - Type: {fileType}, Group: {bank.SupplierGroup}, Name: {file.FileName}, Label: {labelText}");

                        table.Rows.Add(fileType, bank.SupplierGroup, filePath, file.FileName, labelText, DateTime.Now, bank.UploadedBy);
                        labelIndex++;
                    }
                }
            }

            Console.WriteLine("CreateFilesDataTable process completed. Total Rows Added: " + table.Rows.Count);
            return table;
        }

        [HttpPut(template: V)]
        public async Task<IActionResult> UpdateSupplierBankWithFiles([FromForm] SupplierBankWithFilesDto supplierBankWithFiles, [FromForm] string fileIdsString)
        {
            ArgumentNullException.ThrowIfNull(supplierBankWithFiles);

            if (string.IsNullOrEmpty(fileIdsString))
            {
                throw new ArgumentException($"'{nameof(fileIdsString)}' cannot be null or empty.", nameof(fileIdsString));
            }

            try
            {
                var labelTexts = JsonConvert.DeserializeObject<List<string>>(supplierBankWithFiles.LabelTextsJson);
                supplierBankWithFiles.LabelTexts = labelTexts ?? [];

                var fileIds = JsonConvert.DeserializeObject<List<int>>(fileIdsString);

                var filesTable = new DataTable();
                filesTable.Columns.Add("FileType", typeof(string));
                filesTable.Columns.Add("GroupName", typeof(string));
                filesTable.Columns.Add("FilePath", typeof(string));
                filesTable.Columns.Add("FileName", typeof(string));
                filesTable.Columns.Add("LabelText", typeof(string));
                filesTable.Columns.Add("UploadedDate", typeof(DateTime));
                filesTable.Columns.Add("UploadedBy", typeof(int));

                if (supplierBankWithFiles.Files.Count != supplierBankWithFiles.LabelTexts.Count)
                {
                    return BadRequest(new { message = "Mismatch between files and label texts." });
                }

                if (supplierBankWithFiles.Files != null && supplierBankWithFiles.Files.Count > 0)
                {
                    for (int i = 0; i < supplierBankWithFiles.Files.Count; i++)
                    {
                        var file = supplierBankWithFiles.Files[i];
                        if (file.Length > 0)
                        {
                            var uploadsFolder = Path.Combine("uploads", supplierBankWithFiles.SupplierGroup);
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

                            filesTable.Rows.Add(file.ContentType, supplierBankWithFiles.SupplierGroup, filePath, file.FileName, supplierBankWithFiles.LabelTexts[i], DateTime.Now, supplierBankWithFiles.UploadedBy);
                        }
                    }
                }

                using (var connection = new SqlConnection(_appDbContext.Database.GetConnectionString()))
                {
                    using var command = new SqlCommand("UpdateSupplierBankWithFilesX2", connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@SupbankId", supplierBankWithFiles.SupbankId);
                    command.Parameters.AddWithValue("@SupplierId", supplierBankWithFiles.SupplierId);
                    command.Parameters.AddWithValue("@NameBank", supplierBankWithFiles.NameBank);
                    command.Parameters.AddWithValue("@Branch", supplierBankWithFiles.Branch);
                    command.Parameters.AddWithValue("@AccountNum", supplierBankWithFiles.AccountNum);
                    command.Parameters.AddWithValue("@AccountName", supplierBankWithFiles.AccountName);
                    command.Parameters.AddWithValue("@SupplierGroup", supplierBankWithFiles.SupplierGroup);
                    command.Parameters.AddWithValue("@Company", supplierBankWithFiles.Company);

                    var filesParameter = new SqlParameter("@Files", SqlDbType.Structured)
                    {
                        TypeName = "dbo.FileTableType",
                        Value = filesTable
                    };
                    command.Parameters.Add(filesParameter);

                    var fileIdsParameter = new SqlParameter("@FileIds", SqlDbType.VarChar)
                    {
                        Value = fileIdsString
                    };
                    command.Parameters.Add(fileIdsParameter);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }

                return Ok(new { message = "Supplier Bank and files updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error: {ex.Message}" });
            }
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

            if (banks == null || banks.Count == 0)
            {
                return NotFound();
            }

            return Ok(banks);
        }

        [HttpGet("FindSupplierBankBySupplierIDV2")]
        public async Task<IActionResult> FindSupplierBankBySupplierIDV2(int supplierid)
        {
            using var connection = new SqlConnection(_appDbContext.Database.GetConnectionString());
            var parameters = new DynamicParameters();
            parameters.Add("@SupplierId", supplierid, DbType.Int32);

            try
            {
                using var multi = await connection.QueryMultipleAsync(
                    "GetSupplierBankBySupplierIdV3",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var supplierBank = (await multi.ReadAsync<dynamic>()).ToList();

                IEnumerable<dynamic> supplierBankFilesForSupbankId1 = [];
                if (!multi.IsConsumed)
                {
                    supplierBankFilesForSupbankId1 = (await multi.ReadAsync<dynamic>()).ToList();
                }

                IEnumerable<dynamic> supplierBankFilesForSupbankId2 = [];
                if (!multi.IsConsumed)
                {
                    supplierBankFilesForSupbankId2 = (await multi.ReadAsync<dynamic>()).ToList();
                }

                return Ok(new
                {
                    SupplierBank = supplierBank,
                    SupplierBankFilesForSupbankId1 = supplierBankFilesForSupbankId1,
                    SupplierBankFilesForSupbankId2 = supplierBankFilesForSupbankId2
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("GetSupplierBankFileTemplates")]
        public async Task<ActionResult<IEnumerable<SupplierBankFileTemplateDto>>> GetSupplierBankFileTemplateList()
        {
            try
            {
                List<SupplierBankFileTemplateDto> templates;

                using (var connection = new SqlConnection(_appDbContext.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    templates = (await connection.QueryAsync<SupplierBankFileTemplateDto>("EXEC GetSupplierBankFileTemplates")).ToList();
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
    }
}