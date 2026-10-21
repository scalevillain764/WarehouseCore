using Microsoft.EntityFrameworkCore;
using Domain.Models;
namespace Infrastructure
{
    public class WmsDbContext : DbContext 
    {
        public WmsDbContext(DbContextOptions<WmsDbContext> options) : base(options) {}
        public DbSet<Bin> Bins { get; set; }
        public DbSet<WarehouseZone> Zones { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<WarehouseOperation> WarehouseOperations { get; set; }
        public DbSet<WarehouseOperationItem> WarehouseOperationItems { get; set; }
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<Ulid>()
                .HaveConversion<UlidToStringConverter>()
                .HaveMaxLength(26)
                .AreFixedLength();
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
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

            builder.Entity<Warehouse>(entity => // addres is a part of warehouse
            {
                entity.OwnsOne(x => x.Address, address =>
                {
                    address.Property(x => x.Country);
                    address.Property(x => x.City);
                    address.Property(x => x.Street);
                    address.Property(x => x.NumberBuilding);
                });
            });
        }
    }
}