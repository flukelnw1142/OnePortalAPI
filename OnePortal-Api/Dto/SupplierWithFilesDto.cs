namespace OnePortal_Api.Dto
{
    public class SupplierWithFilesDto
    {
        public int? Id { get; set; }
        public required string Prefix { get; set; }
        public required string Name { get; set; }
        public required string Tax_Id { get; set; }
        public required string AddressSup { get; set; }
        public required string District { get; set; }
        public required string Subdistrict { get; set; }
        public required string Province { get; set; }
        public required string PostalCode { get; set; }
        public required string Tel { get; set; }
        public required string Email { get; set; }
        public string? SupplierNum { get; set; }
        public required string SupplierType { get; set; }
        public required string Site { get; set; }
        public string? Vat { get; set; }
        public required string Status { get; set; }
        public required string PaymentMethod { get; set; }
        public int UserId { get; set; }
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public string? EmailUser { get; set; }
        public required string Company { get; set; }
        public required string Type { get; set; }
        public int? OwnerAcc { get; set; }
        public int? OwnerFn { get; set; }
        public required string Mobile { get; set; }
        public int? PostId { get; set; }
        public List<IFormFile> Files { get; set; } = [];
        public string GroupName { get; set; } = string.Empty;
        public string LabelTextsJson { get; set; } = string.Empty;
        public List<string> LabelTexts { get; set; } = [];
        public int UploadedBy { get; set; }
        public string? Telephone { get; set; }
        public List<SupplierFileDto> SupplierFiles { get; set; } = [];
        public List<SupplierBankWithFilesDto> SupplierBanks { get; set; } = [];
    }
}