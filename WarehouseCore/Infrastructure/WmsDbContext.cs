using Microsoft.EntityFrameworkCore;
using Domain.Models;
namespace Infrastructure
{
    public class WmsDbContext : DbContext 
    {
        public WmsDbContext(DbContextOptions<WmsDbContext> options, string schemaName) : base(options) {

            if (string.IsNullOrWhiteSpace(schemaName))
                throw new ArgumentException("Schema name cannot be empty or null", nameof(schemaName));

            _schemaName = schemaName;
        }
        private readonly string _schemaName;
        public DbSet<Bin> Bins { get; set; }
        public DbSet<WarehouseZone> Zones { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.HasDefaultSchema(_schemaName);

            builder.Entity<Warehouse>()
                .HasOne(w => w.Company)
                .WithMany(c => c.Warehouses)
                .HasForeignKey(w => w.CompanyId);

            builder.Entity<User>()
                .HasOne(u => u.Company)
                .WithMany(c => c.Workers)
                .HasForeignKey(u => u.CompanyId);

            builder.Entity<WarehouseZone>()
                .HasOne(z => z.Warehouse)
                .WithMany(w => w.Zones)
                .HasForeignKey(z => z.WarehouseId);

            builder.Entity<Bin>()
                .HasOne(b => b.Zone)
                .WithMany(z => z.Bins)
                .HasForeignKey(b => b.WarehouseZoneId);

            builder.Entity<Bin>()
                .HasMany(b => b.Stocks)
                .WithOne(s => s.Bin)
                .HasForeignKey(s => s.BinId);

            builder.Entity<Stock>()
                .HasOne(s => s.Product)
                .WithMany(p => p.Stocks)
                .HasForeignKey(s => s.ProductId);

            builder.Entity<WarehouseOperationItem>()
                .HasOne(woi => woi.WarehouseOperation)
                .WithMany(wo => wo.Items)
                .HasForeignKey(woi => woi.WarehouseOperationId);

            builder.Entity<WarehouseOperationItem>()
                .HasOne(woi => woi.Product)
                .WithMany(wo => wo.WarehouseOperationItems)
                .HasForeignKey(woi => woi.ProductId);           
        }
    }
}