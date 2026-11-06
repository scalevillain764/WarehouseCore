using Infrastructure.ResponsePatterns;
using Application.Stocks.Requests;
using Application.Stocks.Responses;
using Infrastructure;
using Application.Features;
using Application.CompanyContext;
using Microsoft.EntityFrameworkCore;
using System.Runtime.Intrinsics.X86;
using Domain.Models;
namespace Application.Stocks;
public class StockService : IStockService
{
    public async Task<Result<StockResponse>> CreateStockAsync(WmsDbContext context, CreateStockRequest request, CancellationToken token)
    {
        if (request.Quantity <= 0)
            return Result<StockResponse>.Fail(ErrorType.Validation, "Quantity incorrect");

        var bin = await context.Bins
            .FirstOrDefaultAsync(x => x.Id == request.BinId, token);

        if (bin is null)
            return Result<StockResponse>.Fail(ErrorType.NotFound, "Bin not found");

        var product = await context.Products
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, token);

        if (product is null)
            return Result<StockResponse>.Fail(ErrorType.NotFound, "Product not found");

        var currentProductWeight = request.Quantity * product.WeightPerUnit;

        if (bin.CurrentWeight + currentProductWeight > (int)bin.Type)
            return Result<StockResponse>.Fail(ErrorType.Conflict,
                $"Not enought bin capacity. Max capacity: {(int)bin.Type}, current capacity: {bin.CurrentWeight + currentProductWeight}");

        bool stockExists = await context.Stocks
            .AnyAsync(x => x.ProductId == request.ProductId 
                && x.BinId == request.BinId, token);

        if (stockExists)
            return Result<StockResponse>.Fail(ErrorType.Conflict, "Stock already exists");

        var stock = new Stock(bin.Id, request.ProductId, request.Quantity);

        context.Stocks.Add(stock);
        bin.CurrentWeight += currentProductWeight;

        await context.SaveChangesAsync(token);

        return Result<StockResponse>.Success(new StockResponse(stock));
    }

    public async Task<Result<StockResponse>> IncreaseStockQuantityAsync(WmsDbContext context, EditStockRequest request, CancellationToken token)
    {
        if (request.Quantity <= 0)
            return Result<StockResponse>.Fail(ErrorType.Validation, "Quantity incorrect");

        var bin = await context.Bins
            .FirstOrDefaultAsync(x => x.Id == request.BinId, token);

        if (bin is null)
            return Result<StockResponse>.Fail(ErrorType.NotFound, "Bin not found");

        var product = await context.Products
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, token);

        if (product is null)
            return Result<StockResponse>.Fail(ErrorType.NotFound, "Product not found");

        var stock = await context.Stocks
            .Where(x => x.ProductId == request.ProductId)
            .Where(x => x.BinId == request.BinId)
            .FirstOrDefaultAsync(x => x.Id == request.StockId, token);

        if (stock is null)
            return Result<StockResponse>.Fail(ErrorType.NotFound, "Stock not found");

        var currentProductWeight = request.Quantity * product.WeightPerUnit;

        if (bin.CurrentWeight + currentProductWeight > (int)bin.Type)
            return Result<StockResponse>.Fail(ErrorType.Conflict, 
                $"Not enought bin capacity. Max capacity: {(int)bin.Type}, current capacity: {bin.CurrentWeight + currentProductWeight}");

        stock.Quantity += request.Quantity;
        bin.CurrentWeight += currentProductWeight;

        await context.SaveChangesAsync(token);

        return Result<StockResponse>.Success(new StockResponse(stock));
    }

    public async Task<Result<IEnumerable<StockResponse>>> TransferStockAsync(WmsDbContext context, TransferStockRequest request, CancellationToken token)
    {
        if (request.Quantity <= 0)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Validation, "Quantity incorrect");

        var product = await context.Products
            .FindAsync(request.ProductId, token);

        if (product is null)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.NotFound, "Product not found");

        var sourceBin = await context.Bins
            .Include(x => x.Stocks)
            .Where(x => x.Stocks
                    .Any(s => s.ProductId == request.ProductId))
            .FirstOrDefaultAsync(x => x.Id == request.SourceBinId, token);

        if (sourceBin is null)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.NotFound, "Source bin not found");

        var destinationBin = await context.Bins
            .Include(x => x.Stocks)
            .FirstOrDefaultAsync(x => x.Id == request.DestionationBinId, token);

        if (destinationBin is null)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.NotFound, "Destination bin not found");

        var sourceStock = sourceBin.Stocks
            .FirstOrDefault(x => x.ProductId == request.ProductId);

        if (sourceStock is null)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Conflict, "Something went wrong");

        if (request.Quantity > sourceStock.Quantity)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Conflict, "Source stock doesn't has enough product");

        Stock? destinationStock = destinationBin.Stocks
            .FirstOrDefault(x => x.ProductId == request.ProductId);

        var totalStockWeight = product.WeightPerUnit * request.Quantity;
        var binAvailableWeight = (decimal)(int)destinationBin.Type - destinationBin.CurrentWeight;

        if (totalStockWeight > binAvailableWeight)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Conflict, "Not enough capacity in bin");

        if (destinationStock is null)
            destinationStock = new Stock(destinationBin.Id, request.ProductId, request.Quantity);
        else
            destinationStock.Quantity += request.Quantity;

        sourceStock.Quantity -= request.Quantity;
        destinationBin.CurrentWeight += totalStockWeight;

        return Result<IEnumerable<StockResponse>>.Success([                
            new StockResponse(sourceStock),
            new StockResponse(destinationStock)
        ]);
    }

    public async Task<Result<StockResponse>> ReceiveStockAsync(WmsDbContext context, ReceiveStockRequest request, CancellationToken token)
    {
        if (request.Quantity <= 0)
            return Result<StockResponse>.Fail(ErrorType.Validation, "Quantity incorrect");

        var product = await context.Products
            .FindAsync(request.ProductId, token);

        if (product is null)
            return Result<StockResponse>.Fail(ErrorType.NotFound, "Product not found");

        var destinationBin = await context.Bins
            .Include(x => x.Stocks)
            .Include(x => x.Zone)
            .FirstOrDefaultAsync(x => x.Id == request.DestionationBinId, token);

        if (destinationBin is null)
            return Result<StockResponse>.Fail(ErrorType.NotFound, "Destionation bin not found");

        if (destinationBin.Zone?.Type is not WarehouseZoneType.Receiving)
            return Result<StockResponse>.Fail(ErrorType.Forbidden, "Choose bin from receiving zone");

        Stock? destinationStock = destinationBin.Stocks
            .FirstOrDefault(x => x.ProductId == request.ProductId);

        var totalStockWeight = product.WeightPerUnit * request.Quantity;
        var binAvailableWeight = (decimal)(int)destinationBin.Type - destinationBin.CurrentWeight;

        if (totalStockWeight > binAvailableWeight)
            return Result<StockResponse>.Fail(ErrorType.Conflict, "Not enough capacity in bin");

        if (destinationStock is null)
            destinationStock = new Stock(request.DestionationBinId, request.ProductId, request.Quantity);
        else
            destinationStock.Quantity += request.Quantity;

        destinationBin.CurrentWeight += totalStockWeight;

        return Result<StockResponse>.Success(new StockResponse(destinationStock));
    }

    public async Task<Result<IEnumerable<StockResponse>>> SendStockToSort(WmsDbContext context, TransferStockRequest request, CancellationToken token)
    {
        if (request.Quantity <= 0)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Validation, "Quantity incorrect");

        var product = await context.Products
            .FindAsync(request.ProductId, token);

        if (product is null)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.NotFound, "Product not found");

        var sourceBin = await context.Bins
            .Include(x => x.Stocks)
            .Include(x => x.Zone)
            .Where(x => x.Stocks
                    .Any(s => s.ProductId == request.ProductId))
            .FirstOrDefaultAsync(x => x.Id == request.SourceBinId, token);

        if (sourceBin is null)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.NotFound, "Source bin not found");

        if (sourceBin.Zone?.Type is not WarehouseZoneType.Picking)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Forbidden, "Choose bin from picking");

        var destinationBin = await context.Bins
            .Include(x => x.Stocks)
            .Include(x => x.Zone)
            .FirstOrDefaultAsync(x => x.Id == request.DestionationBinId, token);

        if (destinationBin is null)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.NotFound, "Destination bin not found");

        if (destinationBin.Zone?.Type is not WarehouseZoneType.Sorting)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.NotFound, "Choose destination bin from sorting");

        var sourceStock = sourceBin.Stocks
            .FirstOrDefault(x => x.ProductId == request.ProductId);

        if (sourceStock is null)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Conflict, "Something went wrong");

        if (request.Quantity > sourceStock.Quantity)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Conflict, "Source stock doesn't has enough product");

        Stock? destinationStock = destinationBin.Stocks
            .FirstOrDefault(x => x.ProductId == request.ProductId);

        var totalStockWeight = product.WeightPerUnit * request.Quantity;
        var binAvailableWeight = (decimal)(int)destinationBin.Type - destinationBin.CurrentWeight;

        if (totalStockWeight > binAvailableWeight)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Conflict, "Not enough capacity in bin");

        if (destinationStock is null)
            destinationStock = new Stock(destinationBin.Id, request.ProductId, request.Quantity);
        else
            destinationStock.Quantity += request.Quantity;

        sourceStock.Quantity -= request.Quantity;
        destinationBin.CurrentWeight += totalStockWeight;

        return Result<IEnumerable<StockResponse>>.Success([
            new StockResponse(sourceStock),
            new StockResponse(destinationStock)
        ]);
    }

    public async Task<Result<IEnumerable<StockResponse>>> PickStockAsync(WmsDbContext context, TransferStockRequest request, CancellationToken token)
    {
        if (request.Quantity <= 0)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Validation, "Quantity incorrect");

        var product = await context.Products
            .FindAsync(request.ProductId, token);

        if (product is null)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.NotFound, "Product not found");

        var sourceBin = await context.Bins
            .Include(x => x.Stocks)
            .Include(x => x.Zone)
            .Where(x => x.Stocks
                    .Any(s => s.ProductId == request.ProductId))
            .FirstOrDefaultAsync(x => x.Id == request.SourceBinId, token);

        if (sourceBin is null)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.NotFound, "Source bin not found");

        if (sourceBin.Zone?.Type is not WarehouseZoneType.BulkStorage)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Forbidden, "Choose bin from bulk storage");

        var destinationBin = await context.Bins
            .Include(x => x.Stocks)
            .Include(x => x.Zone)
            .FirstOrDefaultAsync(x => x.Id == request.DestionationBinId, token);

        if (destinationBin is null)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.NotFound, "Destination bin not found");

        if (destinationBin.Zone?.Type is not WarehouseZoneType.Picking)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.NotFound, "Choose destination bin from picking");

        var sourceStock = sourceBin.Stocks
            .FirstOrDefault(x => x.ProductId == request.ProductId);

        if (sourceStock is null)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Conflict, "Something went wrong");

        if (request.Quantity > sourceStock.Quantity)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Conflict, "Source stock doesn't has enough product");

        Stock? destinationStock = destinationBin.Stocks
            .FirstOrDefault(x => x.ProductId == request.ProductId);

        var totalStockWeight = product.WeightPerUnit * request.Quantity;
        var binAvailableWeight = (decimal)(int)destinationBin.Type - destinationBin.CurrentWeight;

        if (totalStockWeight > binAvailableWeight)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Conflict, "Not enough capacity in bin");

        if (destinationStock is null)
            destinationStock = new Stock(destinationBin.Id, request.ProductId, request.Quantity);
        else
            destinationStock.Quantity += request.Quantity;

        sourceStock.Quantity -= request.Quantity;
        destinationBin.CurrentWeight += totalStockWeight;

        return Result<IEnumerable<StockResponse>>.Success([
            new StockResponse(sourceStock),
            new StockResponse(destinationStock)
        ]);
    }

    public async Task<Result<IEnumerable<StockResponse>>> PutawayStockAsync(WmsDbContext context, TransferStockRequest request, CancellationToken token)
    {
        if (request.Quantity <= 0)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Validation, "Quantity incorrect");

        var product = await context.Products
            .FindAsync(request.ProductId, token);

        if (product is null)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.NotFound, "Product not found");

        var sourceBin = await context.Bins
            .Include(x => x.Stocks)
            .Include(x => x.Zone)
            .Where(x => x.Stocks
                    .Any(s => s.ProductId == request.ProductId))
            .FirstOrDefaultAsync(x => x.Id == request.SourceBinId, token);

        if (sourceBin is null)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.NotFound, "Source bin not found");

        if (sourceBin.Zone?.Type is not WarehouseZoneType.Receiving)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Forbidden, "Choose source bin from receiving");

        var destinationBin = await context.Bins
            .Include(x => x.Stocks)
            .Include(x => x.Zone)
            .FirstOrDefaultAsync(x => x.Id == request.DestionationBinId, token);

        if (destinationBin is null)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.NotFound, "Destination bin not found");

        if (destinationBin.Zone?.Type is not WarehouseZoneType.BulkStorage)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Forbidden, "Choose destination bin from bulk storage");

        var sourceStock = sourceBin.Stocks
            .FirstOrDefault(x => x.ProductId == request.ProductId);

        if (sourceStock is null)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Conflict, "Something went wrong");

        if (request.Quantity > sourceStock.Quantity)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Conflict, "Source stock doesn't has enough product");

        Stock? destinationStock = destinationBin.Stocks
            .FirstOrDefault(x => x.ProductId == request.ProductId);

        var totalStockWeight = product.WeightPerUnit * request.Quantity;
        var binAvailableWeight = (decimal)(int)destinationBin.Type - destinationBin.CurrentWeight;

        if (totalStockWeight > binAvailableWeight)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Conflict, "Not enough capacity in bin");

        if (destinationStock is null)
            destinationStock = new Stock(destinationBin.Id, request.ProductId, request.Quantity);
        else
            destinationStock.Quantity += request.Quantity;

        sourceStock.Quantity -= request.Quantity;
        destinationBin.CurrentWeight += totalStockWeight;

        return Result<IEnumerable<StockResponse>>.Success([
            new StockResponse(sourceStock),
            new StockResponse(destinationStock)
        ]);
    }

    public async Task<Result<StockResponse>> DecreaseStockQuantityAsync(WmsDbContext context, EditStockRequest request, CancellationToken token)
    {
        if (request.Quantity <= 0)
            return Result<StockResponse>.Fail(ErrorType.Validation, "Quantity incorrect");

        var bin = await context.Bins
          .FirstOrDefaultAsync(x => x.Id == request.BinId, token);

        if (bin is null)
            return Result<StockResponse>.Fail(ErrorType.NotFound, "Bin not found");

        var product = await context.Products
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, token);

        if (product is null)
            return Result<StockResponse>.Fail(ErrorType.NotFound, "Producgt not found");

        var stock = await context.Stocks
            .Where(x => x.ProductId == request.ProductId)
            .Where(x => x.BinId == request.BinId)
            .FirstOrDefaultAsync(x => x.Id == request.StockId, token);

        if (stock is null)
            return Result<StockResponse>.Fail(ErrorType.NotFound, "Stock not found");

        var stockResponse = new StockResponse(stock);

        if (request.Quantity > stock.Quantity || stock.Quantity - request.Quantity < 0)
            return Result<StockResponse>.Fail(ErrorType.Conflict, $"Incorrect quantity. Current quantity in this bin: {stock.Quantity}");

        var currentProductWeight = request.Quantity * product.WeightPerUnit;

        stock.Quantity -= request.Quantity;
        bin.CurrentWeight -= currentProductWeight;

        if (stock.Quantity == 0) 
            context.Stocks.Remove(stock);

        await context.SaveChangesAsync(token);

        return Result<StockResponse>.Success(stockResponse);
    }
}