using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string TaxId { get; set; }
        public required string AddressSup { get; set; }
        public required string District { get; set; }
        public required string Subdistrict { get; set; }
        public required string Province { get; set; }
        public required string PostalCode { get; set; }
        public required string Tel { get; set; }
        public required string Email { get; set; }
        public required string CustomerNum { get; set; }
        public required string CustomerType { get; set;}
        public required string Site { get; set;}
        public required string Status { get; set; }
        public int UserId { get; set; }
        public required string Company { get; set; }
        public int? OwnerAcc { get; set; }
        public string? Path { get; set; }
        public string? FileReq { get; set; }
        public string? FileCertificate { get; set; }
        public string? Prefix { get; set; }
        public int? PostId { get; set; }
        public string? AddressDetail { get; set; }
        public string? LineId { get; set; }
        public string? FileCertificateATR { get; set; }
        public string? FileOrther { get; set; }
        public string? IsAddressOld { get; set; }
    }
}
