using Microsoft.EntityFrameworkCore;
using Domain.Models;
namespace Infrastructure
{
    public class WmsDbContext : DbContext 
    {
        public WmsDbContext(DbContextOptions<WmsDbContext> options) : base(options) { }
        public DbSet<Bin> Bins { get; set; }
        public DbSet<WarehouseZone> Zones { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
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
        }
    }
}