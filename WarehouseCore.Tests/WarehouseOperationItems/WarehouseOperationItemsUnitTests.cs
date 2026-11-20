using Application.CompanyContext;
using Application.Features;
using Application.Products;
using Application.WarehouseOperationItems;
using Application.WarehouseOperationItems.Requests;
using Application.WarehouseOperationItems.Responses;
using Domain.Models;
using Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using Infrastructure.ResponsePatterns;
using User = Domain.Models.User;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
namespace WarehouseCore.Tests.WarehouseOperationItems;
public class WarehouseOperationItemsUnitTests : MyIAsyncLifetime
{
    private WarehouseOperationItemService _service = null;
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
 
        _service = new WarehouseOperationItemService(
            _factoryMock.Object,
            _validatorMock.Object,
            _companyContextMock.Object);

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

    // Create warehouse operation item
    [Fact]
    public async Task CreateWarehouseOperationItemAsync_ReturnSuccessDTO()
    {
        var company = await AddCompany();
        var currentUser = await AddUser(_userId, company.Id, "Admin1", UserType.Admin);
        var worker = await AddUser(Ulid.NewUlid(), company.Id, "Worker1", UserType.Worker);
        var warehouse = await AddWarehouse(company.Id);
        var product = await AddProduct(10);
        var warehouseZone = await AddWarehouseZone(warehouse.Id);
        var sourceBin = await AddBin(company.Id, warehouseZone.Id, 1);
        var destinationBin = await AddBin(company.Id, warehouseZone.Id, 2);
        var warehouseOperation = await AddWarehouseOperation(WarehouseOperationType.Picking, _userId, worker.Id);

        SetNewCompanySettings(company.Id);
        SetValidatorMockEnsureUserHasAnyRoleSuccess([UserType.Admin, UserType.Manager], company.Id);

        var result = await _service.CreateWarehouseOperationItemAsync(_userId,
            new CreateWarehouseOperationItemsRequest(warehouseOperation.Id, product.Id, 5, 5, sourceBin.Id, destinationBin.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorType);
        Assert.IsAssignableFrom<WarehouseOperationItemsDetailsResponse>(result.Data);

        _validatorMock.Verify(x =>
            x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<UserType>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateWarehouseOperationItemAsync_UserRoleCheckFail_ReturnNotFound()
    {
        var company = await AddCompany();
        var currentUser = await AddUser(_userId, company.Id, "Worker1", UserType.Worker);
        var worker = await AddUser(Ulid.NewUlid(), company.Id, "Worker2", UserType.Worker);
        var warehouse = await AddWarehouse(company.Id);
        var product = await AddProduct(10);
        var warehouseZone = await AddWarehouseZone(warehouse.Id);
        var sourceBin = await AddBin(company.Id, warehouseZone.Id, 1);
        var destinationBin = await AddBin(company.Id, warehouseZone.Id, 2);
        var warehouseOperation = await AddWarehouseOperation(WarehouseOperationType.Picking, _userId, worker.Id);

        SetNewCompanySettings(company.Id);
        SetValidatorMockEnsureUserHasAnyRoleSuccess([UserType.Admin, UserType.Manager], company.Id);

        var result = await _service.CreateWarehouseOperationItemAsync(_userId,
            new CreateWarehouseOperationItemsRequest(warehouseOperation.Id, product.Id, 5, 5, sourceBin.Id, destinationBin.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotNull(result.ErrorType);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);

        _validatorMock.Verify(x =>
            x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<UserType>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateWarehouseOperationItemAsync_WarehouseOperationNotFound_ReturnNotFound()
    {
        var company = await AddCompany();
        var currentUser = await AddUser(_userId, company.Id, "Admin1", UserType.Admin);
        var worker = await AddUser(Ulid.NewUlid(), company.Id, "Worker2", UserType.Worker);
        var warehouse = await AddWarehouse(company.Id);
        var product = await AddProduct(10);
        var warehouseZone = await AddWarehouseZone(warehouse.Id);
        var sourceBin = await AddBin(company.Id, warehouseZone.Id, 1);
        var destinationBin = await AddBin(company.Id, warehouseZone.Id, 2);

        SetNewCompanySettings(company.Id);
        SetValidatorMockEnsureUserHasAnyRoleSuccess([UserType.Admin, UserType.Manager], company.Id);

        var result = await _service.CreateWarehouseOperationItemAsync(_userId,
            new CreateWarehouseOperationItemsRequest(Ulid.NewUlid(), product.Id, 5, 5, sourceBin.Id, destinationBin.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotNull(result.ErrorType);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);

        _validatorMock.Verify(x =>
            x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<UserType>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateWarehouseOperationItemAsync_ProductNotFound_ReturnNotFound()
    {
        var company = await AddCompany();
        var currentUser = await AddUser(_userId, company.Id, "Admin1", UserType.Admin);
        var worker = await AddUser(Ulid.NewUlid(), company.Id, "Worker2", UserType.Worker);
        var warehouse = await AddWarehouse(company.Id);
        var warehouseZone = await AddWarehouseZone(warehouse.Id);
        var sourceBin = await AddBin(company.Id, warehouseZone.Id, 1);
        var destinationBin = await AddBin(company.Id, warehouseZone.Id, 2);
        var warehouseOperation = await AddWarehouseOperation(WarehouseOperationType.Picking, _userId, worker.Id);

        SetNewCompanySettings(company.Id);
        SetValidatorMockEnsureUserHasAnyRoleSuccess([UserType.Admin, UserType.Manager], company.Id);

        var result = await _service.CreateWarehouseOperationItemAsync(_userId,
            new CreateWarehouseOperationItemsRequest(warehouseOperation.Id, Ulid.NewUlid(), 5, 5, sourceBin.Id, destinationBin.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotNull(result.ErrorType);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);

        _validatorMock.Verify(x =>
            x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<UserType>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Remove warehouse operation item
    [Fact]
    public async Task RemoveWarehouseOperationItemAsync_ReturnSuccessDTO()
    {
        var company = await AddCompany();
        var currentUser = await AddUser(_userId, company.Id, "Admin1", UserType.Admin);
        var worker = await AddUser(Ulid.NewUlid(), company.Id, "Worker1", UserType.Worker);
        var warehouse = await AddWarehouse(company.Id);
        var product = await AddProduct(10);
        var warehouseZone = await AddWarehouseZone(warehouse.Id);
        var sourceBin = await AddBin(company.Id, warehouseZone.Id, 1);
        var destinationBin = await AddBin(company.Id, warehouseZone.Id, 2);
        var warehouseOperation = await AddWarehouseOperation(WarehouseOperationType.Picking, _userId, worker.Id);
        var warehouseOperationItem = await AddWarehouseOperationItem(warehouseOperation.Id, product.Id, sourceBin.Id, destinationBin.Id);

        SetNewCompanySettings(company.Id);
        SetValidatorMockEnsureUserHasAnyRoleSuccess([UserType.Admin, UserType.Manager], company.Id);

        var result = await _service.RemoveWarehouseOperationItem(_userId, warehouseOperation.Id, warehouseOperationItem.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorType);
        Assert.IsAssignableFrom<string>(result.Data);

        _validatorMock.Verify(x =>
            x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<UserType>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}