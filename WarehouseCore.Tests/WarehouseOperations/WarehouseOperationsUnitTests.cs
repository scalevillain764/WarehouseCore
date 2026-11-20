using Application.CompanyContext;
using Application.Features;
using Application.Stocks;
using Application.WarehouseOperations;
using Application.WarehouseOperations.Requests;
using Application.WarehouseOperations.Responses;
using Domain.Models;
using User = Domain.Models.User;
using Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Newtonsoft.Json.Linq;
using Infrastructure.ResponsePatterns;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
namespace WarehouseCore.Tests.WarehouseOperations;

public class WarehouseOperationsUnitTests : MyIAsyncLifetime
{
    private Mock<IStockService> _stockServiceMock = null;
    private WarehouseOperationService _warehouseOperationService = null;
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
        _stockServiceMock = new Mock<IStockService>();

        _warehouseOperationService = new WarehouseOperationService(
            _factoryMock.Object,
            _companyContextMock.Object,
            _validatorMock.Object,
            _stockServiceMock.Object
            );

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

    private void SetValidatorMockSuccess(
        IReadOnlyCollection<UserType> types,
        Ulid companyId)
    {
        _validatorMock
            .Setup(x => x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                It.Is<IReadOnlyCollection<UserType>>(roles =>
                    roles != null && roles.Intersect(types).Any()),
                CancellationToken.None))
            .ReturnsAsync(Result<User>.Success(
                new User("example", companyId)));
    }

    private void SetValidatorMockFailure(
        IReadOnlyCollection<UserType> types,
        Ulid companyId,
        ErrorType errorType,
        string errorMessage)
    {
        _validatorMock
            .Setup(x => x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                It.Is<IReadOnlyCollection<UserType>>(roles =>
                    roles != null && roles.Intersect(types).Any()),
                CancellationToken.None))
            .ReturnsAsync(Result<User>.Fail(errorType, errorMessage));
    }

    [Fact]
    public async Task CreateWarehouseOperation_ReturnSuccessDTO()
    {
        var company = await AddCompany();
        var assignedUser = await AddUser(Ulid.NewUlid(), company.Id, "AssignedUser", UserType.Worker);
        var currentUser = await AddUser(_userId, company.Id, "CurrentUser", UserType.Admin);
        var warehouse = await AddWarehouse(company.Id);
  
        _factoryMock
            .Setup(x => x.GetContextAsync(company.Id, CancellationToken.None))
            .ReturnsAsync(_wmsDbContext);

        _companyContextMock
            .SetupGet(x => x.CompanyId)
            .Returns(company.Id);

        SetValidatorMockSuccess([UserType.Admin, UserType.Worker], company.Id);

        var result = await _warehouseOperationService.CreateWarehouseOperationAsync(_userId, assignedUser.Id,
            new CreateWarehouseOperationRequest(WarehouseOperationType.Picking), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorType);
        Assert.IsAssignableFrom<WarehouseOperationDetailsResponse>(result.Data);

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
    public async Task CreateWarehouseOperation_UserRoleCheckFailure_ReturnNotFound()
    {
        var company = await AddCompany();
        var assignedUser = await AddUser(Ulid.NewUlid(), company.Id, "AssignedUser", UserType.Worker);
        var currentUser = await AddUser(_userId, company.Id, "CurrentUser", UserType.Admin);

        _factoryMock
            .Setup(x => x.GetContextAsync(company.Id, CancellationToken.None))
            .ReturnsAsync(_wmsDbContext);

        _companyContextMock
            .SetupGet(x => x.CompanyId)
            .Returns(company.Id);

        SetValidatorMockSuccess([UserType.Admin, UserType.Worker], company.Id);

        var result = await _warehouseOperationService.CreateWarehouseOperationAsync(_userId, assignedUser.Id,
            new CreateWarehouseOperationRequest(WarehouseOperationType.Picking), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotNull(result.ErrorType);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CreateWarehouseOperation_EnsureAssignedUserExistsFailure_ReturnConflict()
    {
        var company = await AddCompany();
        var assignedUser = await AddUser(Ulid.NewUlid(), company.Id, "AssignedUser", UserType.Worker);
        var currentUser = await AddUser(_userId, company.Id, "CurrentUser", UserType.Admin);

        _factoryMock
            .Setup(x => x.GetContextAsync(company.Id, CancellationToken.None))
            .ReturnsAsync(_wmsDbContext);

        _companyContextMock
            .SetupGet(x => x.CompanyId)
            .Returns(company.Id);

        _validatorMock
            .Setup(x => x.EnsureUserHasAnyRoleAsync(_wmsDbContext, _userId, "Only admins and managers can create warehouse operation",
                new List<UserType> { UserType.Admin, UserType.Manager }, CancellationToken.None))
            .ReturnsAsync(Result<User>.Success(new User("example", company.Id)));

        _validatorMock
            .Setup(x => x.EnsureUserExistsAsync(_wmsDbContext, assignedUser.Id, CancellationToken.None))
            .ReturnsAsync(false);

        var result = await _warehouseOperationService.CreateWarehouseOperationAsync(_userId, assignedUser.Id,
            new CreateWarehouseOperationRequest(WarehouseOperationType.Picking), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotNull(result.ErrorType);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}