using Infrastructure.ResponsePatterns;
using Application.Stocks.Requests;
using Application.Stocks.Responses;
namespace Application.Stocks;
public interface IStockService
{
    Task<Result<StockResponse>> CreateStockAsync(Ulid userId, CreateStockRequest request, CancellationToken token);
}
