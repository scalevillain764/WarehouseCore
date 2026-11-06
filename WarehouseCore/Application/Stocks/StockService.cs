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
    public async Task<Result<StockResponse>> AddStockAsync(WmsDbContext context, CreateStockRequest request, CancellationToken token)
    {
        if (request.Quantity <= 0)
            return Result<StockResponse>.Fail(ErrorType.Validation, "Quantity incorrect");

        var bin = await context.Bins
            .FirstOrDefaultAsync(x => x.Id == request.BinId, token);

        if (bin is null)
            return Result<StockResponse>.Fail(ErrorType.NotFound, "Bin not found");

        var product = await context.Products
            .FindAsync(request.ProductId, token);

        if (product is null)
            return Result<StockResponse>.Fail(ErrorType.NotFound, "Product not found");

        var currentProductWeight = request.Quantity * product.WeightPerUnit;

        if (bin.CurrentWeight + currentProductWeight > (int)bin.Type)
            return Result<StockResponse>.Fail(ErrorType.Conflict,
                $"Not enought bin capacity. Max capacity: {(int)bin.Type}, current capacity: {bin.CurrentWeight + currentProductWeight}");

        Stock? currentStock = await context.Stocks
            .Where(x => x.BinId == request.BinId)
            .FirstOrDefaultAsync(x => x.ProductId == request.ProductId, token);

        if (currentStock is null)
            currentStock = new Stock(bin.Id, request.ProductId, request.Quantity);
        else
            currentStock.Quantity += request.Quantity;
        
        context.Stocks.Add(currentStock);
        bin.CurrentWeight += currentProductWeight;

        return Result<StockResponse>.Success(new StockResponse(currentStock));
    }

    public async Task<Result<IEnumerable<StockResponse>>> TransferStockAsync(WmsDbContext context, TransferStockRequest request, CancellationToken token)
    {
        if (request.Quantity <= 0)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Validation, "Quantity incorrect");

        var product = await context.Products
            .FindAsync(request.ProductId, token);

        if (request.SourceBinId == request.DestionationBinId)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Conflict, "Choose different bins");

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
            .Where(x => x.BinId == request.SourceBinId)
            .FirstOrDefault(x => x.ProductId == request.ProductId);

        if (sourceStock is null)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Conflict, "Something went wrong");

        if (request.Quantity > sourceStock.Quantity)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Conflict, "Source stock doesn't has enough product");

        Stock? destinationStock = destinationBin.Stocks
            .Where(x => x.BinId == request.DestionationBinId)
            .FirstOrDefault(x => x.ProductId == request.ProductId);

        var totalStockWeight = product.WeightPerUnit * request.Quantity;
        var binAvailableWeight = (decimal)(int)destinationBin.Type - destinationBin.CurrentWeight;

        if (totalStockWeight > binAvailableWeight)
            return Result<IEnumerable<StockResponse>>.Fail(ErrorType.Conflict, "Not enough capacity in bin");

        if (destinationStock is null)
        {
            destinationStock = new Stock(destinationBin.Id, request.ProductId, request.Quantity);
            context.Stocks.Add(destinationStock);
        }
        else
            destinationStock.Quantity += request.Quantity;

        sourceStock.Quantity -= request.Quantity;
        sourceBin.CurrentWeight -= totalStockWeight;
        destinationBin.CurrentWeight += totalStockWeight;

        if (sourceStock.Quantity <= 0)
            context.Stocks.Remove(sourceStock);

        return Result<IEnumerable<StockResponse>>.Success([                
            new StockResponse(sourceStock),
            new StockResponse(destinationStock)
        ]);
    }

    public async Task<Result<StockResponse>> RemoveStockAsync(WmsDbContext context, EditStockRequest request, CancellationToken token)
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
            .FirstOrDefaultAsync(x => x.ProductId == request.ProductId, token);

        if (stock is null)
            return Result<StockResponse>.Fail(ErrorType.NotFound, "Stock not found");

        var stockResponse = new StockResponse(stock);

        if (request.Quantity > stock.Quantity)
            return Result<StockResponse>.Fail(ErrorType.Conflict, $"Incorrect quantity. Current quantity in this bin: {stock.Quantity}");

        var currentProductWeight = request.Quantity * product.WeightPerUnit;

        stock.Quantity -= request.Quantity;
        bin.CurrentWeight -= currentProductWeight;

        if (stock.Quantity == 0) 
            context.Stocks.Remove(stock);

        return Result<StockResponse>.Success(stockResponse);
    }
}