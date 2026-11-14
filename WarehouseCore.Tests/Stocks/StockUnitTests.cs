using Application.CompanyContext;
using Application.Features;
using Application.Products;
using Application.Stocks;
using Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Domain.Models;
using Xunit;
using Application.Stocks.Responses;
namespace WarehouseCore.Tests.Stocks;

public class StockUnitTests : MyIAsyncLifetime
{
    private IStockService _stockService;
    public override async ValueTask InitializeAsync()
    {
        _registryConnection = new SqliteConnection("Data Source=:memory:");
        await _registryConnection.OpenAsync();

        var registryOptions = new DbContextOptionsBuilder<RegistryDbContext>()
            .UseSqlite(_registryConnection)
            .Options;

        _registryDbContext = new RegistryDbContext(registryOptions);
        await _registryDbContext.Database.EnsureCreatedAsync();

        _wmsConnection = new SqliteConnection("Data Source=:memory:");
        await _wmsConnection.OpenAsync();

        var wmsOptions = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(_wmsConnection)
            .Options;

        _wmsDbContext = new WmsDbContext(wmsOptions);
        await _wmsDbContext.Database.EnsureCreatedAsync();

        _userId = Ulid.NewUlid();
        _companyContextMock = new Mock<ICurrentCompanyContext>();
        _factoryMock = new Mock<IWmsDbContextFactory>();
        _validatorMock = new Mock<ICompanyAcessValidator>();

        _validatorMock
            .Setup(x => x.EnsureUserExistsAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _stockService = new StockService();

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

    private async Task<Bin> AddBin(Ulid companyId, Ulid warehousezoneId)
    {
        var bin = new Bin("PCK-01-01-01", 1, 1, 1, companyId, warehousezoneId, BinTypeByCapacity.Heavy);
        _wmsDbContext.Bins.Add(bin);
        await _wmsDbContext.SaveChangesAsync(CancellationToken.None);
        return bin;
    }

    private async Task<Product> AddProduct()
    {
        var product = new Product(
            "Example_product",
            "Example_description",
            ProductCategory.AudioDevices,
            "HyperX",
            UnitOfMeasure.Set,
            120,
            "Black");

        _wmsDbContext.Products.Add(product);
        await _wmsDbContext.SaveChangesAsync(CancellationToken.None);

        return product;
    }

    private async Task<Company> AddCompany()
    {
        var company = new Company("company1", "CM1");

        _registryDbContext.Companies.Add(company);
        await _registryDbContext.SaveChangesAsync(CancellationToken.None);

        return company;
    }

    private void SetNewCompanySettings(Ulid newCompanyId)
    {
        _companyContextMock
          .SetupGet(x => x.CompanyId)
              .Returns(newCompanyId);

        _factoryMock
            .Setup(x => x.GetContextAsync(newCompanyId, CancellationToken.None))
                .ReturnsAsync(_wmsDbContext);
    }

    private async Task<Warehouse> AddWarehouse(Ulid companyId)
    {
        var warehouse = new Warehouse("example_warehouse", new Address("Belarus", "Minsk", "Nesterova", "92"), companyId);
        _wmsDbContext.Warehouses.Add(warehouse);
        await _wmsDbContext.SaveChangesAsync(CancellationToken.None);
        return warehouse;
    }

    private async Task<WarehouseZone> AddWarehouseZone(Ulid companyId, Ulid warehouseId)
    {
        var warehouseZone = new WarehouseZone("Picking", "Example", warehouseId, WarehouseZoneType.Picking);
        _wmsDbContext.WarehouseZones.Add(warehouseZone);
        await _wmsDbContext.SaveChangesAsync(CancellationToken.None);
        return warehouseZone;
    }

    [Fact]
    public async Task AddStockAsync_ReturnSuccessDTO()
    {
        var company = await AddCompany();
        var product = await AddProduct();
        var warehouse = await AddWarehouse(company.Id);
        var warehouseZone = await AddWarehouseZone(company.Id, warehouse.Id);
        var bin = await AddBin(company.Id, warehouseZone.Id);

        SetNewCompanySettings(company.Id);

        var result = await _stockService.AddStockAsync(_wmsDbContext,
            new Application.Stocks.Requests.CreateStockRequest(bin.Id, product.Id, 1), 
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorType);
        Assert.IsAssignableFrom<StockResponse>(result.Data);
    }
}