using Infrastructure.ResponsePatterns;
using Application.Stocks.Requests;
using Application.Stocks.Responses;
using Infrastructure;
namespace Application.Stocks;

public interface IStockService
{
    Task<Result<StockResponse>> AddStockAsync(WmsDbContext context, CreateStockRequest request, CancellationToken token);
    Task<Result<StockResponse>> RemoveStockAsync(WmsDbContext context, EditStockRequest request, CancellationToken token);
    Task<Result<IEnumerable<StockResponse>>> TransferStockAsync(WmsDbContext context, TransferStockRequest request, CancellationToken token);
}
