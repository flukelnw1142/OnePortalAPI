namespace OnePortal_Api.Dto
{
    public class SupplierFileDto
    {
        public int FileId { get; set; }
        public int SupplierId { get; set; }
        public required string FileType { get; set; }
        public required string GroupName { get; set; }
        public required string FilePath { get; set; }
        public required string FileName { get; set; }
        public required string LabelText { get; set; }
        public DateTime UploadedDate { get; set; }
        public int UploadedBy { get; set; }
    }
}
