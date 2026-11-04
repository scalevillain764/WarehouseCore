namespace Application.Stocks.Requests;

public sealed record ReceiveStockRequest(Ulid ProductId, Ulid DestionationBinId, int Quantity);