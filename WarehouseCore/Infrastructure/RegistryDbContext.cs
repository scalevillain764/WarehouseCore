using Microsoft.EntityFrameworkCore;
using Domain.Models;

namespace Infrastructure
{
    public class RegistryDbContext : DbContext
    {
        public DbSet<Company> Companies { get; set; }
        public RegistryDbContext(DbContextOptions<RegistryDbContext> options) : base(options) { }
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<Ulid>()
                .HaveConversion<UlidToStringConverter>()
                .HaveMaxLength(26)
                .AreFixedLength();
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Company>()
                .HasIndex(c => c.Code)
                .IsUnique();
        }
    }
}