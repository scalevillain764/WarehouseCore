using Application.Companies;
using Application.CompanyContext;
using Application.Features;
using Application.Products;
using Application.Products.Requests;
using Application.Products.Responses;
using Domain.Models;
using Infrastructure;
using Infrastructure.ResponsePatterns;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Newtonsoft.Json.Linq;
using WarehouseCore.Tests;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using User = Domain.Models.User;
namespace WarehouseCore.Tests.Products;
public class ProductUnitTests : MyIAsyncLifetime
{
    private IProductService _productService;
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

        _productService = new ProductService(
            _factoryMock.Object,
            _companyContextMock.Object,
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

    private async Task<Company> AddCompany()
    {
        var company = new Company("company1", "CM1");
        _registryDbContext.Companies.Add(company);
        await _registryDbContext.SaveChangesAsync(CancellationToken.None);
        return company;
    }

    private async Task SetCompaniesSettings(Ulid companyId)
    {
        _factoryMock
            .Setup(x => x.GetContextAsync(companyId, CancellationToken.None))
            .ReturnsAsync(_wmsDbContext);

        _companyContextMock
            .SetupGet(x => x.CompanyId)
            .Returns(companyId);
    }

    // Add product

    [Fact]
    public async Task AddProductAsync_ReturnSuccessDTO()
    {
        var company = await AddCompany();

        await SetCompaniesSettings(company.Id);

        _validatorMock
            .Setup(x => x.EnsureUserHasAnyRoleAsync(_wmsDbContext, _userId, It.IsAny<string>(),
                It.Is<IReadOnlyCollection<UserType>>(roles => roles.Count == 2 && roles.Contains(UserType.Admin) 
                    && roles.Contains(UserType.Manager)), CancellationToken.None))
            .ReturnsAsync(Result<User>.Success(new User("example", company.Id)));

        var result = await _productService
            .AddProductAsync(_userId,
            new CreateProductRequest(
                "Example_Product",
                "Description",
                ProductCategory.Accessories,
                "Apple",
                UnitOfMeasure.Pcs,
                13,
                null),
            CancellationToken.None);

        Assert.True(result?.IsSuccess);
        Assert.NotNull(result?.Data);
        Assert.IsType<ProductDetailsResponse>(result?.Data);

        _validatorMock.Verify(x =>
            x.EnsureUserHasAnyRoleAsync(_wmsDbContext,
                _userId,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<UserType>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddProductAsync_UserRoleFail_ReturnFailure()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        _validatorMock
            .Setup(x => x.EnsureUserHasAnyRoleAsync(_wmsDbContext, _userId, It.IsAny<string>(),
                It.Is<IReadOnlyCollection<UserType>>(roles => roles.Count == 2 && roles.Contains(UserType.Admin)
                    && roles.Contains(UserType.Manager)), CancellationToken.None))
            .ReturnsAsync(Result<User>.Fail(ErrorType.NotFound, "User not found"));

        var result = await _productService
            .AddProductAsync(_userId,
            new CreateProductRequest(
                "Example_Product",
                "Description",
                ProductCategory.Accessories,
                "Apple",
                UnitOfMeasure.Pcs,
                13,
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotNull(result.ErrorType);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);

        _validatorMock.Verify(x =>
        x.EnsureUserHasAnyRoleAsync(_wmsDbContext,
            _userId,
            It.IsAny<string>(),
            It.IsAny<IReadOnlyCollection<UserType>>(),
            It.IsAny<CancellationToken>()),
        Times.Once);
    }

    // Remove product

    [Fact]
    public async Task
}
