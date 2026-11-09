namespace Application.Stocks.Requests;
public sealed record EditStockRequest(Ulid BinId, Ulid ProductId, int Quantity);