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
            .FirstOrDefaultAsync(x => x.Id == request.BinId);

        if (bin is null)
            return Result<StockResponse>.Fail(ErrorType.NotFound, "Bin not found");

        var product = await context.Products
            .FirstOrDefaultAsync(x => x.Id == request.ProductId);

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

    public async Task<Result<StockResponse>> DecreaseStockQuantityAsync(WmsDbContext context, EditStockRequest request, CancellationToken token)
    {
        if (request.Quantity <= 0)
            return Result<StockResponse>.Fail(ErrorType.Validation, "Quantity incorrect");

        var bin = await context.Bins
          .FirstOrDefaultAsync(x => x.Id == request.BinId);

        if (bin is null)
            return Result<StockResponse>.Fail(ErrorType.NotFound, "Bin not found");

        var product = await context.Products
            .FirstOrDefaultAsync(x => x.Id == request.ProductId);

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
            return Result<StockResponse>.Fail(ErrorType.Conflict, $"Incorrect quantity. Current quantity in this bib: {stock.Quantity}");

        var currentProductWeight = request.Quantity * product.WeightPerUnit;

        stock.Quantity -= request.Quantity;
        bin.CurrentWeight -= currentProductWeight;

        if (stock.Quantity == 0) 
            context.Stocks.Remove(stock);

        await context.SaveChangesAsync(token);

        return Result<StockResponse>.Success(stockResponse);
    }
}