using Application.Stocks;
using Application.Stocks.Requests;
using Application.Stocks.Responses;
using Domain.Models;
using Infrastructure;
using Infrastructure.ResponsePatterns;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WarehouseCore.Tests.Stocks;

public class StockUnitTests : MyIAsyncLifetime
{
    private StockService _stockService = null!;

    public override async ValueTask InitializeAsync()
    {
        _wmsConnection = new SqliteConnection("Data Source=:memory:");
        await _wmsConnection.OpenAsync();

        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(_wmsConnection)
            .Options;

        _wmsDbContext = new WmsDbContext(options);
        await _wmsDbContext.Database.EnsureCreatedAsync();

        _stockService = new StockService();
    }

    public override async ValueTask DisposeAsync()
    {
        await _wmsDbContext.DisposeAsync();
        await _wmsConnection.DisposeAsync();

        await Task.CompletedTask;
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

    private async Task<Warehouse> AddWarehouse()
    {
        var warehouse = new Warehouse(
            $"warehouse_{Ulid.NewUlid()}",
            new Address("Belarus", "Minsk", "Nesterova", "92"),
            Ulid.NewUlid());

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

    // Add stock

    [Fact]
    public async Task AddStockAsync_ReturnSuccessDTO()
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);
        var bin = await AddBin(warehouse.CompanyId, zone.Id);

        var result = await _stockService.AddStockAsync(
            _wmsDbContext,
            new CreateStockRequest(bin.Id, product.Id, 1),
            CancellationToken.None);

        var stock = _wmsDbContext.Stocks.Local
            .FirstOrDefault(x => x.BinId == bin.Id && x.ProductId == product.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.IsType<StockResponse>(result.Data);
        Assert.NotNull(stock);
        Assert.Equal(1, stock.Quantity);
        Assert.Equal(10, bin.CurrentWeight);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task AddStockAsync_IncorrectQuantity_ReturnValidation(int quantity)
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);
        var bin = await AddBin(warehouse.CompanyId, zone.Id);

        var result = await _stockService.AddStockAsync(
            _wmsDbContext,
            new CreateStockRequest(bin.Id, product.Id, quantity),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Quantity incorrect", result.ErrorMessage);
        Assert.Equal(0, bin.CurrentWeight);
    }

    [Fact]
    public async Task AddStockAsync_BinNotFound_ReturnNotFound()
    {
        var product = await AddProduct();

        var result = await _stockService.AddStockAsync(
            _wmsDbContext,
            new CreateStockRequest(Ulid.NewUlid(), product.Id, 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Bin not found", result.ErrorMessage);
    }

    [Fact]
    public async Task AddStockAsync_ProductNotFound_ReturnNotFound()
    {
        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);
        var bin = await AddBin(warehouse.CompanyId, zone.Id);

        var result = await _stockService.AddStockAsync(
            _wmsDbContext,
            new CreateStockRequest(bin.Id, Ulid.NewUlid(), 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Product not found", result.ErrorMessage);
    }

    [Fact]
    public async Task AddStockAsync_BinCapacityExceeded_ReturnConflict()
    {
        var product = await AddProduct(120);

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);
        var bin = await AddBin(
            warehouse.CompanyId,
            zone.Id,
            capacity: BinTypeByCapacity.Light);

        var result = await _stockService.AddStockAsync(
            _wmsDbContext,
            new CreateStockRequest(bin.Id, product.Id, 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(0, bin.CurrentWeight);
    }

    [Fact]
    public async Task AddStockAsync_ExistingStock_IncreaseQuantity()
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);
        var bin = await AddBin(warehouse.CompanyId, zone.Id);

        var stock = await AddStock(bin, product, 5);

        var result = await _stockService.AddStockAsync(
            _wmsDbContext,
            new CreateStockRequest(bin.Id, product.Id, 3),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(8, stock.Quantity);
        Assert.Equal(80, bin.CurrentWeight);
        Assert.Single(_wmsDbContext.Stocks.Local);
    }

    // Transfer stock

    [Fact]
    public async Task TransferStockAsync_ReturnSuccess()
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);

        var sourceBin = await AddBin(warehouse.CompanyId, zone.Id, 1);
        var destinationBin = await AddBin(warehouse.CompanyId, zone.Id, 2);

        var sourceStock = await AddStock(sourceBin, product, 10);

        var result = await _stockService.TransferStockAsync(
            _wmsDbContext,
            new TransferStockRequest(
                sourceBin.Id,
                destinationBin.Id,
                product.Id,
                4),
            CancellationToken.None);

        var destinationStock = _wmsDbContext.Stocks.Local
            .FirstOrDefault(x => x.BinId == destinationBin.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count());
        Assert.Equal(6, sourceStock.Quantity);
        Assert.NotNull(destinationStock);
        Assert.Equal(4, destinationStock.Quantity);
        Assert.Equal(60, sourceBin.CurrentWeight);
        Assert.Equal(40, destinationBin.CurrentWeight);
    }

    [Fact]
    public async Task TransferStockAsync_TransferAllQuantity_RemoveSourceStock()
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);

        var sourceBin = await AddBin(warehouse.CompanyId, zone.Id, 1);
        var destinationBin = await AddBin(warehouse.CompanyId, zone.Id, 2);

        var sourceStock = await AddStock(sourceBin, product, 3);

        var result = await _stockService.TransferStockAsync(
            _wmsDbContext,
            new TransferStockRequest(
                sourceBin.Id,
                destinationBin.Id,
                product.Id,
                3),
            CancellationToken.None);

        var destinationStock = _wmsDbContext.Stocks.Local
            .FirstOrDefault(x => x.BinId == destinationBin.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(destinationStock);
        Assert.Equal(3, destinationStock.Quantity);
        Assert.Equal(0, sourceBin.CurrentWeight);
        Assert.Equal(30, destinationBin.CurrentWeight);
        Assert.Equal(
            EntityState.Deleted,
            _wmsDbContext.Entry(sourceStock).State);
    }

    [Fact]
    public async Task TransferStockAsync_ExistingDestinationStock_IncreaseQuantity()
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);

        var sourceBin = await AddBin(warehouse.CompanyId, zone.Id, 1);
        var destinationBin = await AddBin(warehouse.CompanyId, zone.Id, 2);

        var sourceStock = await AddStock(sourceBin, product, 5);
        var destinationStock = await AddStock(destinationBin, product, 2);

        var result = await _stockService.TransferStockAsync(
            _wmsDbContext,
            new TransferStockRequest(
                sourceBin.Id,
                destinationBin.Id,
                product.Id,
                3),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, sourceStock.Quantity);
        Assert.Equal(5, destinationStock.Quantity);
        Assert.Equal(20, sourceBin.CurrentWeight);
        Assert.Equal(50, destinationBin.CurrentWeight);
        Assert.Equal(2, _wmsDbContext.Stocks.Local.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task TransferStockAsync_IncorrectQuantity_ReturnValidation(int quantity)
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);

        var sourceBin = await AddBin(warehouse.CompanyId, zone.Id, 1);
        var destinationBin = await AddBin(warehouse.CompanyId, zone.Id, 2);

        await AddStock(sourceBin, product, 5);

        var result = await _stockService.TransferStockAsync(
            _wmsDbContext,
            new TransferStockRequest(
                sourceBin.Id,
                destinationBin.Id,
                product.Id,
                quantity),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task TransferStockAsync_SameBins_ReturnConflict()
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);
        var bin = await AddBin(warehouse.CompanyId, zone.Id);

        await AddStock(bin, product, 5);

        var result = await _stockService.TransferStockAsync(
            _wmsDbContext,
            new TransferStockRequest(
                bin.Id,
                bin.Id,
                product.Id,
                1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("Choose different bins", result.ErrorMessage);
    }

    [Fact]
    public async Task TransferStockAsync_ProductNotFound_ReturnNotFound()
    {
        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);

        var sourceBin = await AddBin(warehouse.CompanyId, zone.Id, 1);
        var destinationBin = await AddBin(warehouse.CompanyId, zone.Id, 2);

        var result = await _stockService.TransferStockAsync(
            _wmsDbContext,
            new TransferStockRequest(
                sourceBin.Id,
                destinationBin.Id,
                Ulid.NewUlid(),
                1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Product not found", result.ErrorMessage);
    }

    [Fact]
    public async Task TransferStockAsync_SourceBinNotFound_ReturnNotFound()
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);
        var destinationBin = await AddBin(warehouse.CompanyId, zone.Id);

        var result = await _stockService.TransferStockAsync(
            _wmsDbContext,
            new TransferStockRequest(
                Ulid.NewUlid(),
                destinationBin.Id,
                product.Id,
                1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Source bin not found", result.ErrorMessage);
    }

    [Fact]
    public async Task TransferStockAsync_DestinationBinNotFound_ReturnNotFound()
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);
        var sourceBin = await AddBin(warehouse.CompanyId, zone.Id);

        await AddStock(sourceBin, product, 5);

        var result = await _stockService.TransferStockAsync(
            _wmsDbContext,
            new TransferStockRequest(
                sourceBin.Id,
                Ulid.NewUlid(),
                product.Id,
                1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Destination bin not found", result.ErrorMessage);
    }

    [Fact]
    public async Task TransferStockAsync_StockNotFound_ReturnNotFound()
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);

        var sourceBin = await AddBin(warehouse.CompanyId, zone.Id, 1);
        var destinationBin = await AddBin(warehouse.CompanyId, zone.Id, 2);

        var result = await _stockService.TransferStockAsync(
            _wmsDbContext,
            new TransferStockRequest(
                sourceBin.Id,
                destinationBin.Id,
                product.Id,
                1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Source bin not found", result.ErrorMessage);
    }

    [Fact]
    public async Task TransferStockAsync_InsufficientStock_ReturnConflict()
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);

        var sourceBin = await AddBin(warehouse.CompanyId, zone.Id, 1);
        var destinationBin = await AddBin(warehouse.CompanyId, zone.Id, 2);

        await AddStock(sourceBin, product, 2);

        var result = await _stockService.TransferStockAsync(
            _wmsDbContext,
            new TransferStockRequest(
                sourceBin.Id,
                destinationBin.Id,
                product.Id,
                3),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("Source stock doesn't has enough product", result.ErrorMessage);
    }

    [Fact]
    public async Task TransferStockAsync_DestinationCapacityExceeded_ReturnConflict()
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);

        var sourceBin = await AddBin(warehouse.CompanyId, zone.Id, 1);
        var destinationBin = await AddBin(
            warehouse.CompanyId,
            zone.Id,
            2,
            BinTypeByCapacity.Light);

        await AddStock(sourceBin, product, 3);
        await AddStock(destinationBin, product, 9);

        var result = await _stockService.TransferStockAsync(
            _wmsDbContext,
            new TransferStockRequest(
                sourceBin.Id,
                destinationBin.Id,
                product.Id,
                2),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("Not enough capacity in bin", result.ErrorMessage);
        Assert.Equal(30, sourceBin.CurrentWeight);
        Assert.Equal(90, destinationBin.CurrentWeight);
    }

    // Remove stock

    [Fact]
    public async Task RemoveStockAsync_ReturnSuccess()
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);
        var bin = await AddBin(warehouse.CompanyId, zone.Id);

        var stock = await AddStock(bin, product, 10);

        var result = await _stockService.RemoveStockAsync(
            _wmsDbContext,
            new EditStockRequest(bin.Id, product.Id, 4),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(6, stock.Quantity);
        Assert.Equal(60, bin.CurrentWeight);
    }

    [Fact]
    public async Task RemoveStockAsync_RemoveAllQuantity_DeleteStock()
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);
        var bin = await AddBin(warehouse.CompanyId, zone.Id);

        var stock = await AddStock(bin, product, 5);

        var result = await _stockService.RemoveStockAsync(
            _wmsDbContext,
            new EditStockRequest(bin.Id, product.Id, 5),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(0, bin.CurrentWeight);
        Assert.Equal(
            EntityState.Deleted,
            _wmsDbContext.Entry(stock).State);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task RemoveStockAsync_IncorrectQuantity_ReturnValidation(int quantity)
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);
        var bin = await AddBin(warehouse.CompanyId, zone.Id);

        await AddStock(bin, product, 5);

        var result = await _stockService.RemoveStockAsync(
            _wmsDbContext,
            new EditStockRequest(bin.Id, product.Id, quantity),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task RemoveStockAsync_BinNotFound_ReturnNotFound()
    {
        var product = await AddProduct();

        var result = await _stockService.RemoveStockAsync(
            _wmsDbContext,
            new EditStockRequest(Ulid.NewUlid(), product.Id, 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Bin not found", result.ErrorMessage);
    }

    [Fact]
    public async Task RemoveStockAsync_ProductNotFound_ReturnNotFound()
    {
        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);
        var bin = await AddBin(warehouse.CompanyId, zone.Id);

        var result = await _stockService.RemoveStockAsync(
            _wmsDbContext,
            new EditStockRequest(bin.Id, Ulid.NewUlid(), 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Product not found", result.ErrorMessage);
    }

    [Fact]
    public async Task RemoveStockAsync_StockNotFound_ReturnNotFound()
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);
        var bin = await AddBin(warehouse.CompanyId, zone.Id);

        var result = await _stockService.RemoveStockAsync(
            _wmsDbContext,
            new EditStockRequest(bin.Id, product.Id, 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Stock not found", result.ErrorMessage);
    }

    [Fact]
    public async Task RemoveStockAsync_QuantityExceedsStock_ReturnConflict()
    {
        var product = await AddProduct();

        var warehouse = await AddWarehouse();
        var zone = await AddWarehouseZone(warehouse.Id);
        var bin = await AddBin(warehouse.CompanyId, zone.Id);

        var stock = await AddStock(bin, product, 3);

        var result = await _stockService.RemoveStockAsync(
            _wmsDbContext,
            new EditStockRequest(bin.Id, product.Id, 4),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Incorrect quantity. Current quantity in this bin: 3",
            result.ErrorMessage);
        Assert.Equal(3, stock.Quantity);
        Assert.Equal(30, bin.CurrentWeight);
    }
}
