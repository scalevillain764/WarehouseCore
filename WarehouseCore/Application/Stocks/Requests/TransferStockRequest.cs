namespace Application.Stocks.Requests;

public sealed record TransferStockRequest(Ulid SourceBinId, Ulid DestionationBinId, Ulid ProductId, int Quantity);