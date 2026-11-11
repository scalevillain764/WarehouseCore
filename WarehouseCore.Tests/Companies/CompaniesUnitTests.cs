using Application.Bins;
using Application.Companies;
using Application.Companies.Responses;
using Application.CompanyContext;
using Application.Features;
using Castle.Components.DictionaryAdapter.Xml;
using Domain.Models;
using Infrastructure;
using Infrastructure.ResponsePatterns;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
namespace WarehouseCore.Tests.Companies;

public class CompaniesUnitTests : IAsyncLifetime
{
    private SqliteConnection _registryConnection = null!;
    private RegistryDbContext _registryDbContext = null!;

    private SqliteConnection _wmsConnection = null!;
    private WmsDbContext _wmsDbContext = null!;

    private Mock<ICurrentCompanyContext> _companyContextMock = null!;
    private Mock<IWmsDbContextFactory> _factoryMock = null!;
    private Mock<ICompanyAcessValidator> _validatorMock = null!;

    private CompanyService _companyService = null!;
    private Ulid _userId;

    public async ValueTask InitializeAsync()
    {
        // Registry
        _registryConnection = new SqliteConnection("Data Source=:memory:");
        await _registryConnection.OpenAsync();
        var registryOptions = new DbContextOptionsBuilder<RegistryDbContext>()
            .UseSqlite(_registryConnection)
            .Options;
        _registryDbContext = new RegistryDbContext(registryOptions);
        await _registryDbContext.Database.EnsureCreatedAsync();

        // WMS DB
        _wmsConnection = new SqliteConnection("Data Source=:memory:");
        await _wmsConnection.OpenAsync();
        var wmsOptions = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(_wmsConnection)
            .Options;
        _wmsDbContext = new WmsDbContext(wmsOptions);
        await _wmsDbContext.Database.EnsureCreatedAsync();

        // Mocks & services
        _userId = Ulid.NewUlid();
        _companyContextMock = new Mock<ICurrentCompanyContext>();
        _factoryMock = new Mock<IWmsDbContextFactory>();
        _validatorMock = new Mock<ICompanyAcessValidator>();

        // Validator default behavior
        _validatorMock
            .Setup(x => x.EnsureUserExistsAsync(_wmsDbContext, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _companyService = new CompanyService(
            _factoryMock.Object,
            _companyContextMock.Object,
            _registryDbContext,
            _validatorMock.Object);

        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _registryDbContext.DisposeAsync();
        await _registryConnection.DisposeAsync();
        await _wmsDbContext.DisposeAsync();
        await _wmsConnection.DisposeAsync();
        await Task.CompletedTask;
    }

    // Get current company

    [Fact]
    public async Task GetCurrentCompanyAsync_CompanyDoesntExist_ReturnFailure()
    {
    }

    [Fact]
    public async Task GetCurrentCompanyAsync_CompanyExists_ReturnsSuccessWithSuccessDTO()
    {
        var newCompany = new Company("OOO Коммунарка", "KMN");
        _registryDbContext.Companies.Add(newCompany);

        await _registryDbContext.SaveChangesAsync(CancellationToken.None);

        _companyContextMock
            .SetupGet(x => x.CompanyId)
                .Returns(newCompany.Id);

        _factoryMock.
            Setup(x => x.GetContextAsync(newCompany.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_wmsDbContext);

        var result = await _companyService
            .GetCurrentCompanyAsync(_userId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(newCompany.Id, result.Data?.Id);

        // Validator 
        _validatorMock.Verify(
            x => x.EnsureUserExistsAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Edit current company

    [Fact]
    public async Task EditCurrentCompanyAsync_UserNotEnoughPermissions_ReturnFailure()
    {
        var newCompany = new Company("OOO Коммунарка", "KMN");
        _registryDbContext.Companies.Add(newCompany);

        await _registryDbContext.SaveChangesAsync(CancellationToken.None);

        _companyContextMock
            .SetupGet(x => x.CompanyId)
                .Returns(newCompany.Id);

        _factoryMock.
            Setup(x => x.GetContextAsync(newCompany.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_wmsDbContext);

        string error_msg = "Only admin can edit company properties";

        _validatorMock
            .Setup(x => 
                x.EnsureUserIsAdminAsync(_wmsDbContext, _userId, error_msg, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<User>.Fail(ErrorType.NotFound, error_msg));

        var result = await _companyService.EditCurrentCompanyAsync(_userId,
            new Application.Companies.Requests.EditCompanyRequest("example"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(newCompany.Id, result.Data?.Id);

        _validatorMock.Verify(
           x => x.EnsureUserExistsAsync(
               _wmsDbContext,
               _userId,
               It.IsAny<CancellationToken>()),
           Times.Once);
    }

    [Fact]
    public async Task EditCurrentCompanyAsync_CompanyDoesntExist_ReturnFailure()
    {
        _companyContextMock
            .SetupGet(x => x.CompanyId)
                .Returns(Ulid.NewUlid());

        _factoryMock.
            Setup(x => x
                .GetContextAsync(Ulid.NewUlid(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_wmsDbContext);

        var result = await _companyService.EditCurrentCompanyAsync(_userId,
            new Application.Companies.Requests.EditCompanyRequest("example"), CancellationToken.None);

        Assert.False(result.IsSuccess);    
        Assert.Equal(ErrorType.NotFound, result.ErrorType);

        _validatorMock.Verify(
           x => x.EnsureUserExistsAsync(
               _wmsDbContext,
               _userId,
               It.IsAny<CancellationToken>()),
           Times.Never);
    }
}
