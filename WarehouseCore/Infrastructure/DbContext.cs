using Microsoft.EntityFrameworkCore;
using Domain.Models;
namespace Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Bin> Bins { get; set; }
        public DbSet<WarehouseZone> Zones { get; set; }
    }
}