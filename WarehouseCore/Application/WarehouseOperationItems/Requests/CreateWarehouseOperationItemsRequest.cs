namespace Application.WarehouseOperationItems.Requests;
public sealed record CreateWarehouseOperationItemsRequest(
    Ulid warehouseOperationId,
    Ulid productId,
    int PlannedQuantity,
    int ProcessedQuantity,
    Ulid sourceBinId,
    Ulid destinationBinId);