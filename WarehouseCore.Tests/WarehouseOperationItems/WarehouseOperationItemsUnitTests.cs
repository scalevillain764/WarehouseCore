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
    private async Task<Company> AddCompany()
    {
        var company = new Company("company1", "CM1");

        _registryDbContext.Companies.Add(company);
        await _registryDbContext.SaveChangesAsync(CancellationToken.None);

        return company;
    }

    private async Task<Product> AddProduct(int weightPerUnit = 10)
    {
        var product = new Product(
            "Example_product",
            "Example_description",
            ProductCategory.AudioDevices,
            "HyperX",
            UnitOfMeasure.Set,
            weightPerUnit,
            "Black");

        _wmsDbContext.Products.Add(product);
        await _wmsDbContext.SaveChangesAsync();

        return product;
    }

    private async Task<User> AddUser(Ulid id, Ulid companyId, string firstName, UserType type)
    {
        var user = new User(id, firstName, companyId, type);
        _wmsDbContext.Users.Add(user);
        await _wmsDbContext.SaveChangesAsync(CancellationToken.None);
        return user;
    }
    private async Task<Warehouse> AddWarehouse(Ulid companyId)
    {
        var warehouse = new Warehouse(
            $"warehouse_{Ulid.NewUlid()}",
            new Address("Belarus", "Minsk", "Nesterova", "92"),
            companyId);

        _wmsDbContext.Warehouses.Add(warehouse);
        await _wmsDbContext.SaveChangesAsync();

        return warehouse;
    }

    private async Task<WarehouseZone> AddWarehouseZone(Ulid warehouseId)
    {
        var zone = new WarehouseZone(
            "Picking",
            "Example",
            warehouseId,
            WarehouseZoneType.Picking);

        _wmsDbContext.WarehouseZones.Add(zone);
        await _wmsDbContext.SaveChangesAsync();

        return zone;
    }

    private async Task<Bin> AddBin(
        Ulid companyId,
        Ulid warehouseZoneId,
        int binNumber = 1,
        BinTypeByCapacity capacity = BinTypeByCapacity.Heavy)
    {
        var bin = new Bin(
            $"PCK-01-01-{binNumber:D2}",
            1,
            1,
            binNumber,
            companyId,
            warehouseZoneId,
            capacity);

        _wmsDbContext.Bins.Add(bin);
        await _wmsDbContext.SaveChangesAsync();

        return bin;
    }

    private async Task<Stock> AddStock(
        Bin bin,
        Product product,
        int quantity)
    {
        var stock = new Stock(bin.Id, product.Id, quantity);

        bin.CurrentWeight += quantity * product.WeightPerUnit;

        _wmsDbContext.Stocks.Add(stock);
        await _wmsDbContext.SaveChangesAsync();

        return stock;
    }

    private async Task<WarehouseOperation> AddWarehouseOperation(
        WarehouseOperationType type, Ulid createdById, Ulid assignedToId
        )
    {
        var warehouseOperation = new WarehouseOperation(WarehouseOperationType.Picking, createdById, assignedToId);
        _wmsDbContext.WarehouseOperations.Add(warehouseOperation);
        await _wmsDbContext.SaveChangesAsync(CancellationToken.None);
        return warehouseOperation;
    }

    private async Task<WarehouseOperationItem> AddWarehouseOperationItem(
        Ulid warehouseOperationId, Ulid productId, Ulid sourceBinId, Ulid destinationBinId
        )
    {
        var warehouseOperationItem = new WarehouseOperationItem(warehouseOperationId, productId, 1, sourceBinId, destinationBinId);
        _wmsDbContext.WarehouseOperationItems.Add(warehouseOperationItem);
        await _wmsDbContext.SaveChangesAsync(CancellationToken.None);
        return warehouseOperationItem;
    }

    private void SetCompaniesSettings(Ulid companyId)
    {
        _factoryMock
            .Setup(x => x.GetContextAsync(companyId, CancellationToken.None))
            .ReturnsAsync(_wmsDbContext);

        _companyContextMock
            .SetupGet(x => x.CompanyId)
            .Returns(companyId);
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

        SetCompaniesSettings(company.Id);

        _validatorMock
            .Setup(x => x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext, currentUser.Id, "Only admins and managers can create warehouse operation item",
            new List<UserType> { UserType.Admin, UserType.Manager }, CancellationToken.None))
            .ReturnsAsync(Result<User>.Success(new User("user_example", company.Id)));

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

        SetCompaniesSettings(company.Id);

        _validatorMock
            .Setup(x => x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext, currentUser.Id, "Only admins and managers can create warehouse operation item",
            new List<UserType> { UserType.Admin, UserType.Manager }, CancellationToken.None))
            .ReturnsAsync(Result<User>.Fail(ErrorType.NotFound, "User not found"));

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

        SetCompaniesSettings(company.Id);

        _validatorMock
            .Setup(x => x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext, currentUser.Id, "Only admins and managers can create warehouse operation item",
            new List<UserType> { UserType.Admin, UserType.Manager }, CancellationToken.None))
            .ReturnsAsync(Result<User>.Success(new User("user_example", company.Id)));

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

        SetCompaniesSettings(company.Id);

        _validatorMock
            .Setup(x => x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext, currentUser.Id, "Only admins and managers can create warehouse operation item",
            new List<UserType> { UserType.Admin, UserType.Manager }, CancellationToken.None))
            .ReturnsAsync(Result<User>.Success(new User("user_example", company.Id)));

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

        SetCompaniesSettings(company.Id);

        _validatorMock
            .Setup(x => x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext, currentUser.Id, "Only admins and managers can remove warehouse operation item",
            new List<UserType> { UserType.Admin, UserType.Manager }, CancellationToken.None))
            .ReturnsAsync(Result<User>.Success(new User("user_example", company.Id)));

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