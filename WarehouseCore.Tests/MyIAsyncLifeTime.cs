using Application.Companies;
using Application.CompanyContext;
using Application.Features;
using Infrastructure;
using Microsoft.Data.Sqlite;
using Moq;
using Xunit;
namespace WarehouseCore.Tests;
public abstract class MyIAsyncLifetime : IAsyncLifetime
{
    protected SqliteConnection _registryConnection { get; set; } = null!;
    protected RegistryDbContext _registryDbContext { get; set; } = null!;
    protected SqliteConnection _wmsConnection { get; set; } = null!;
    protected WmsDbContext _wmsDbContext { get; set; } = null!;
    protected Mock<ICurrentCompanyContext> _companyContextMock { get; set; } = null!;
    protected Mock<IWmsDbContextFactory> _factoryMock { get; set; } = null!;
    protected Mock<ICompanyAcessValidator> _validatorMock { get; set; } = null!;
    protected Ulid _userId { get; set; }
    public abstract ValueTask InitializeAsync();
    public abstract ValueTask DisposeAsync();
}