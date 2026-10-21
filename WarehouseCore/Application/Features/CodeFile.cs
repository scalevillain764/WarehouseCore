using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WarehouseCore;
namespace Application.Features;

public class DesignTimeWmsDbFactory : IDesignTimeDbContextFactory<WmsDbContext>
{
    public WmsDbContext CreateDbContext(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Program>(); }) 
            .Build();

        IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

        var connectionString = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidDataException("Connection string can not be empty");

        var optionsBuilder = new DbContextOptionsBuilder<WmsDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new WmsDbContext(optionsBuilder.Options, "default_schema");
    }
}