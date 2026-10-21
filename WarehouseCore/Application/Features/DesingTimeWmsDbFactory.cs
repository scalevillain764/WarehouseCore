using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WarehouseCore;
namespace Application.Features;

public class DesignTimeWmsDbFactory : IDesignTimeDbContextFactory<WmsDbContext>
{
    public WmsDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidDataException("Connection string can not be empty");

        var optionsBuilder = new DbContextOptionsBuilder<WmsDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new WmsDbContext(optionsBuilder.Options);
    }
}