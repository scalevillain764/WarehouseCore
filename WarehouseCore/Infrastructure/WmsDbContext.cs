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
            builder.Entity<>
        }
    }
}