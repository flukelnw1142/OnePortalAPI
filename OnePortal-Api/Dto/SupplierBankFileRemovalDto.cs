namespace OnePortal_Api.Dto
{
    public class SupplierBankFileRemovalDto
    {
        public int SupbankId { get; set; }
        public int FileId { get; set; }
        public bool IsNewUpload { get; set; }
    }
}