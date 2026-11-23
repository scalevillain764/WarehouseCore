using Application.Companies;
using Application.CompanyContext;
using Application.Features;
using Domain.Models;
using Infrastructure;
using Infrastructure.ResponsePatterns;
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

    // Helpers
    protected async Task<Company> AddCompany()
    {
        var company = new Company("company1", "CM1");

        _registryDbContext.Companies.Add(company);
        await _registryDbContext.SaveChangesAsync(CancellationToken.None);

        return company;
    }

    protected async Task<Product> AddProduct(int weightPerUnit = 10)
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
        await _wmsDbContext.SaveChangesAsync(CancellationToken.None);

        return product;
    }

    protected async Task<User> AddUser(Ulid id, Ulid companyId, string firstName, UserType type)
    {
        var user = new User(id, firstName, companyId, type);
        _wmsDbContext.Users.Add(user);
        await _wmsDbContext.SaveChangesAsync(CancellationToken.None);
        return user;
    }

    protected async Task<Warehouse> AddWarehouse(Ulid companyId)
    {
        var warehouse = new Warehouse(
            $"warehouse_{Ulid.NewUlid()}",
            new Address("Belarus", "Minsk", "Nesterova", "92"),
            companyId);

        _wmsDbContext.Warehouses.Add(warehouse);
        await _wmsDbContext.SaveChangesAsync();

        return warehouse;
    }

    protected async Task<Warehouse> AddWarehouse()
    {
        var warehouse = new Warehouse(
            $"warehouse_{Ulid.NewUlid()}",
            new Address("Belarus", "Minsk", "Nesterova", "92"),
            Ulid.NewUlid());

        _wmsDbContext.Warehouses.Add(warehouse);
        await _wmsDbContext.SaveChangesAsync();

        return warehouse;
    }

    protected async Task<WarehouseZone> AddWarehouseZone(Ulid warehouseId)
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

    protected async Task<Bin> AddBin(
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

    protected async Task<Stock> AddStock(
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

    protected void SetValidatorMockEnsureUserHasAnyRoleSuccess(
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

    protected void SetValidatorMockEnsureUserHasAnyRoleFailure(IReadOnlyCollection<UserType> types, Ulid companyId, 
        ErrorType errorType, string errorMessage)
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

    protected void SetValidatorMocknEsureUserExistsTrue(Ulid userId)
    {
        _validatorMock
            .Setup(x => x.EnsureUserExistsAsync(_wmsDbContext, userId, CancellationToken.None))
            .ReturnsAsync(true);
    }
    protected void SetValidatorMocknEsureUserExistsFalse(Ulid userId)
    {
        _validatorMock
            .Setup(x => x.EnsureUserExistsAsync(_wmsDbContext, userId, CancellationToken.None))
            .ReturnsAsync(false);
    }


    protected void SetNewCompanySettings(Ulid newCompanyId)
    {
        _companyContextMock
          .SetupGet(x => x.CompanyId)
              .Returns(newCompanyId);

        _factoryMock
            .Setup(x => x.GetContextAsync(newCompanyId, CancellationToken.None))
                .ReturnsAsync(_wmsDbContext);
    }

    protected async Task<WarehouseOperation> AddWarehouseOperation(
        WarehouseOperationType type, Ulid createdById, Ulid assignedToId
        )
    {
        var warehouseOperation = new WarehouseOperation(type, createdById, assignedToId);
        _wmsDbContext.WarehouseOperations.Add(warehouseOperation);
        await _wmsDbContext.SaveChangesAsync(CancellationToken.None);
        return warehouseOperation;
    }

    protected async Task<WarehouseOperationItem> AddWarehouseOperationItem(
        Ulid warehouseOperationId, Ulid productId, Ulid sourceBinId, Ulid destinationBinId
        )
    {
        var warehouseOperationItem = new WarehouseOperationItem(warehouseOperationId, productId, 1, sourceBinId, destinationBinId);
        _wmsDbContext.WarehouseOperationItems.Add(warehouseOperationItem);
        await _wmsDbContext.SaveChangesAsync(CancellationToken.None);
        return warehouseOperationItem;
    }
}