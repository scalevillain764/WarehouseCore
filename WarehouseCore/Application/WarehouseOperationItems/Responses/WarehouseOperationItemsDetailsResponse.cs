namespace Application.WarehouseOperationItems.Responses;

public sealed record WarehouseOperationItemsDetailsResponse(
    Ulid Id,
    Ulid warehouseOperationId,
    Ulid productId,
    string productName,
    int PlannedQuantity,
    int ProcessedQuantity,
    Ulid sourceBinId,
    string sourceBinCode,
    Ulid destinationBinId,
    string destinationBinCode
    );