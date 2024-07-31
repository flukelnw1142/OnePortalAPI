using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Model;

namespace OnePortal_Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> User { get; set; }
        public DbSet<Role> Role { get; set; }
        public DbSet<Customer> Customer { get; set; }
        public DbSet<Supplier> Supplier { get; set; }
        public DbSet<PostCode> PostCode { get; set; }
        public DbSet<SupplierTypeMasterData> supplierTypeMasters { get; set; }
        public DbSet<CustomerTypeMasterData> customerTypeMasters { get; set; }
        public DbSet<SupplierBank> supplierBanks { get; set; }
        public DbSet<BankMasterData> bankMasters { get; set; }
        public DbSet<EventLog> eventLogs { get; set; }
        public DbSet<PaymentMethod> paymentMethods { get; set; }
        public DbSet<Vat> vats { get; set; }
        public DbSet<Company> companies { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupDetail> GroupDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(u => u.user_id);
            modelBuilder.Entity<Customer>().ToTable("Customer_info", "dbo");
            modelBuilder.Entity<Supplier>().ToTable("Supplier_info", "dbo");
            modelBuilder.Entity<PostCode>().ToTable("MS_postCode");
            modelBuilder.Entity<SupplierTypeMasterData>().ToTable("MS_supplier_type", "dbo");
            modelBuilder.Entity<CustomerTypeMasterData>().ToTable("MS_customer_type", "dbo");
            modelBuilder.Entity<SupplierBank>().ToTable("supplier_Bank", "dbo");
            modelBuilder.Entity<SupplierBank>().Property(s => s.supplier_id).ValueGeneratedNever();
            modelBuilder.Entity<BankMasterData>().ToTable("MS_bank", "dbo");
            modelBuilder.Entity<EventLog>().ToTable("event_log", "dbo");
            modelBuilder.Entity<PaymentMethod>().ToTable("MS_payment_method", "dbo");
            modelBuilder.Entity<Vat>().ToTable("MS_vat", "dbo");
            modelBuilder.Entity<Company>().ToTable("MS_company", "dbo");
            modelBuilder.Entity<Group>().ToTable("Ms_Group", "dbo");
            modelBuilder.Entity<GroupDetail>().ToTable("Ms_Group_detail", "dbo");


            modelBuilder.Entity<Group>()
        .HasMany(g => g.groupDetails)
        .WithOne(gd => gd.Group)
        .HasForeignKey(gd => gd.group_id);

            base.OnModelCreating(modelBuilder);
        }

    }
}
