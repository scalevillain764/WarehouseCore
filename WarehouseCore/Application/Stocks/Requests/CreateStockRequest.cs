namespace Application.Stocks.Requests;

public sealed record CreateStockRequest(Ulid BinId, Ulid ProductId, int Quantity);