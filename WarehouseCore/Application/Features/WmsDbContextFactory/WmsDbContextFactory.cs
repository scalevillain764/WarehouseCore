using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace Application.Features;

public class WmsDbContextFactory : IWmsDbContextFactory
{
    private readonly RegistryDbContext _registryDbContext;
    private readonly IConfiguration _configuration;
    public WmsDbContextFactory(RegistryDbContext registryDbContext, IConfiguration configuration)
    {
        _registryDbContext = registryDbContext;
        _configuration = configuration;
    }
    public async Task<WmsDbContext> GetContextAsync(Ulid CompanyId, CancellationToken token)
    {
        var company = await _registryDbContext.Companies
            .Where(x => x.IsActive)
            .FirstOrDefaultAsync(x => x.Id == CompanyId, token);

        if (company is null)
            throw new InvalidOperationException("Company not found");

        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        var connectionStringBuilder =
            new NpgsqlConnectionStringBuilder(connectionString)
            {
                SearchPath = company.SchemaName
            };

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string cannot be null or empty");

        var optionsBuilder =
            new DbContextOptionsBuilder<WmsDbContext>();

        optionsBuilder.UseNpgsql(
            connectionStringBuilder.ConnectionString);

        var newContext = new WmsDbContext(optionsBuilder.Options);

        return newContext;
    }
}