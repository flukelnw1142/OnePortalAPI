namespace OnePortal_Api.Dto
{
    public class CustomerSupplierDto
    {
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
        public required string Status { get; set; }
        public  string? Num { get; set; } // Customer หรือ Supplier Number
        public required string Type { get; set; } // Customer หรือ Supplier Type
        public required string Site { get; set; }
        public required string PaymentMethod { get; set; }
        public required string Source { get; set; } // เพื่อบอกว่าเป็น Customer หรือ Supplier
        public int UserId { get; set; }
        public required string Company { get; set; }
        public int? OwnerACC { get; set; }
        public int? OwnerFN { get; set; }
        public required string Mobile { get; set; }
        public required string SupplierGroup { get; set; }
        public string? OwnerAccName { get; set; }
        public string? OwnerFnName { get; set; }
        public string? RejectReason { get; set; }

    }
}

