using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Dto;
using OnePortal_Api.Model;

namespace OnePortal_Api.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> User { get; set; }
        public DbSet<Role> Role { get; set; }
        public DbSet<Customer> Customer { get; set; }
        public DbSet<Supplier> Supplier { get; set; }
        public DbSet<SupplierFile> SupplierFiles { get; set; }
        public DbSet<SupplierBankFile> SupplierBankFiles { get; set; }
        public DbSet<PostCode> PostCode { get; set; }
        public DbSet<SupplierTypeMasterData> SupplierTypeMasterData { get; set; }
        public DbSet<CustomerTypeMasterData> CustomerTypeMasterData { get; set; }
        public DbSet<SupplierBank> SupplierBanks { get; set; }
        public DbSet<BankMasterData> BankMasterData { get; set; }
        public DbSet<EventLog> EventLogs { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Vat> Vats { get; set; }
        public DbSet<Company> Company { get; set; }
        public DbSet<Group> Group { get; set; }
        public DbSet<GroupDetail> GroupDetails { get; set; }
        public DbSet<CustomerSupplierDto> CustomerSupplierDto { get; set; }
        public DbSet<PrefixMasterData> Prefix { get; set; }
        public DbSet<TempNumKey> TempNumKeys { get; set; }
        public DbSet<UserResponsible> userResponsibles { get; set; }
        public DbSet<PDPAConsent> pDPAConsents { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<MasterContent> masterContents { get; set; }
        public DbSet<Announcement_Consent> announcement_Consents { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(u => u.UserId);
            modelBuilder.Entity<Customer>().ToTable("Customer_info", "dbo");
            modelBuilder.Entity<Supplier>().ToTable("Supplier_info", "dbo");
            modelBuilder.Entity<PostCode>().ToTable("MS_postCode");
            modelBuilder.Entity<SupplierTypeMasterData>().ToTable("MS_supplier_type", "dbo");
            modelBuilder.Entity<CustomerTypeMasterData>().ToTable("MS_customer_type", "dbo");
            modelBuilder.Entity<SupplierBank>().ToTable("supplier_Bank", "dbo");
            modelBuilder.Entity<SupplierBank>().Property(s => s.SupplierId).ValueGeneratedNever();
            modelBuilder.Entity<BankMasterData>().ToTable("MS_bank", "dbo");
            modelBuilder.Entity<EventLog>().ToTable("event_log", "dbo");
            modelBuilder.Entity<PaymentMethod>().ToTable("MS_payment_method", "dbo");
            modelBuilder.Entity<Vat>().ToTable("MS_vat", "dbo");
            modelBuilder.Entity<Company>().ToTable("MS_company", "dbo");
            modelBuilder.Entity<Group>().ToTable("Ms_Group", "dbo");
            modelBuilder.Entity<GroupDetail>().ToTable("Ms_Group_detail", "dbo");
            modelBuilder.Entity<TempNumKey>().ToTable("TempNumKey", "dbo");
            modelBuilder.Entity<UserResponsible>().ToTable("MS_UserResponsible", "dbo");
            modelBuilder.Entity<PDPAConsent>().ToTable("PDPA_Consent", "dbo");
            modelBuilder.Entity<EmailLog>().ToTable("EmailLog", "dbo");
            modelBuilder.Entity<MasterContent>().ToTable("MasterContent", "dbo");
            modelBuilder.Entity<Announcement_Consent>().ToTable("Announcement_Consent", "dbo");
            modelBuilder.Entity<Group>()
        .HasMany(g => g.GroupDetails)
        .WithOne(gd => gd.Group)
        .HasForeignKey(gd => gd.GroupId);

            base.OnModelCreating(modelBuilder);
        }

    }
}
