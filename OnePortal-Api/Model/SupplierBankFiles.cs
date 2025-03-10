namespace OnePortal_Api.Model
{
    public class SupplierBankFile
    {
        public int Id { get; set; }
        public int SupbankId { get; set; }
        public required string FileType { get; set; }
        public required string GroupName { get; set; }
        public required string FilePath { get; set; }
        public required string FileName { get; set; }
        public DateTime UploadedDate { get; set; }
        public int? UploadedBy { get; set; }
        public required SupplierBank SupplierBank { get; set; }
    }
}