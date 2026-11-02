using Infrastructure.ResponsePatterns;
using Application.Stocks.Requests;
using Application.Stocks.Responses;
using Infrastructure;
namespace Application.Stocks;
public interface IStockService
{
    Task<Result<StockResponse>> CreateStockAsync(WmsDbContext context, CreateStockRequest request, CancellationToken token);
    Task<Result<StockResponse>> IncreaseStockQuantityAsync(WmsDbContext context, EditStockRequest request, CancellationToken token);
    Task<Result<StockResponse>> DecreaseStockQuantityAsync(WmsDbContext context, EditStockRequest request, CancellationToken token);
}
