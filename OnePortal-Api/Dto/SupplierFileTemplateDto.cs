namespace OnePortal_Api.Dto
{
    public class SupplierFileTemplateDto
    {
        public int TemplateId { get; set; }
        public required string GroupName { get; set; }
        public required string FileName { get; set; }
        public required string FileType { get; set; }
        public required string LabelText { get; set; }
        public required string FilePath { get; set; }
    }
}
