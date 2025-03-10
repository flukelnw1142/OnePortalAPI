using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Model;

namespace OnePortal_Api.Data
{
    public class OracleDbContext : DbContext
    {
        public OracleDbContext(DbContextOptions<OracleDbContext> options)
            : base(options)
        {
        }
        public DbSet<ViewData> XoneKeySupplierV { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ViewData>(entity =>
            {
                // แผนที่ไปยัง view ใน Oracle
                entity.ToTable("XONE_KEY_SUPPPLIER_V");
            });
        }
    }
}
