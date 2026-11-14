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

public class CompaniesUnitTests : MyIAsyncLifetime
{ 
    private ICompanyService _companyService = null!;

    public override async ValueTask InitializeAsync()
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

    public override async ValueTask DisposeAsync()
    {
        await _registryDbContext.DisposeAsync();
        await _registryConnection.DisposeAsync();
        await _wmsDbContext.DisposeAsync();
        await _wmsConnection.DisposeAsync();
        await Task.CompletedTask;
    }

    // Helpers
    private void SetNewCompanySettings(Ulid newCompanyId)
    {
        _companyContextMock
          .SetupGet(x => x.CompanyId)
              .Returns(newCompanyId);

        _factoryMock
            .Setup(x => x.GetContextAsync(newCompanyId, CancellationToken.None))
                .ReturnsAsync(_wmsDbContext);
    }    

    // Get current company

    [Fact]
    public async Task GetCurrentCompanyAsync_CompanyDoesntExist_ReturnFailure()
    {
        var newCompany = new Company("a", "b");;

        SetNewCompanySettings(newCompany.Id);

        _validatorMock
            .Setup(x => x.EnsureUserExistsAsync(_wmsDbContext, _userId, CancellationToken.None))
            .ReturnsAsync(true);

        var result = await _companyService
            .GetCurrentCompanyAsync(_userId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetCurrentCompanyAsync_CompanyExists_ReturnsSuccessWithSuccessDTO()
    {
        var newCompany = new Company("OOO Коммунарка", "KMN");
        _registryDbContext.Companies.Add(newCompany);

        await _registryDbContext.SaveChangesAsync(CancellationToken.None);

        SetNewCompanySettings(newCompany.Id);

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

    [Fact]
    public async Task GetCurrentCompanyAsync_UserDoesntBelongToCompany_ReturnFailure()
    {
        var newCompany = new Company("OOO Коммунарка", "KMN");
        _registryDbContext.Companies.Add(newCompany);

        await _registryDbContext.SaveChangesAsync(CancellationToken.None);

        SetNewCompanySettings(newCompany.Id);

        _validatorMock
            .Setup(x => x.EnsureUserExistsAsync(_wmsDbContext, _userId, CancellationToken.None))
            .ReturnsAsync(false);

        var rez = await _companyService
            .GetCurrentCompanyAsync(_userId, CancellationToken.None);

        Assert.False(rez.IsSuccess);
        Assert.Equal(ErrorType.Conflict, rez.ErrorType);
        Assert.Null(rez.Data);
    }

    // Edit current company

    [Fact]
    public async Task EditCurrentCompanyAsync_UserNotEnoughPermissions_ReturnFailure()
    {
        var newCompany = new Company("OOO Коммунарка", "KMN");
        _registryDbContext.Companies.Add(newCompany);

        await _registryDbContext.SaveChangesAsync(CancellationToken.None);

        SetNewCompanySettings(newCompany.Id);

        _validatorMock
            .Setup(x => 
                x.EnsureUserIsAdminAsync(_wmsDbContext, _userId, It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync(Result<User>.Fail(ErrorType.NotFound, It.IsAny<string>()));

        var result = await _companyService.EditCurrentCompanyAsync(_userId,
            new Application.Companies.Requests.EditCompanyRequest("example"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);

        _validatorMock.Verify(
           x => x.EnsureUserIsAdminAsync(
               _wmsDbContext,
               _userId,
               It.IsAny<string>(),
               It.IsAny<CancellationToken>()),
           Times.Once);
    }

    [Fact]
    public async Task EditCurrentCompany_SameCompanyNameExists_ReturnFailure()
    {
        var company_one = new Company("a1", "b");
        var company_two = new Company("a2", "c");

        _registryDbContext.Companies.Add(company_one);
        _registryDbContext.Companies.Add(company_two);

        await _registryDbContext.SaveChangesAsync(CancellationToken.None);

        SetNewCompanySettings(company_two.Id);

        _validatorMock
            .Setup(x =>
                x.EnsureUserIsAdminAsync(_wmsDbContext, _userId, It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync(Result<User>.Success(new User("example", company_two.Id)));

        var result = await _companyService
            .EditCurrentCompanyAsync(_userId, new Application.Companies.Requests.EditCompanyRequest("a1"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);

        _validatorMock.Verify(
         x => x.EnsureUserIsAdminAsync(
             _wmsDbContext,
             _userId,
             It.IsAny<string>(),
             It.IsAny<CancellationToken>()),
         Times.Once);
    }

    [Fact]
    public async Task EditCurrentCompanyAsync_CompanyDoesntExist_ReturnFailure()
    {
        var newCompany = new Company("a", "b");

        SetNewCompanySettings(newCompany.Id);

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
