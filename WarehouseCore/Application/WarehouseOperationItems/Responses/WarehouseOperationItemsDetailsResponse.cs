using Domain.Models;
namespace Application.WarehouseOperationItems.Responses;

public sealed record WarehouseOperationItemsDetailsResponse(
    Ulid Id,
    Ulid warehouseOperationId,
    Ulid productId,
    string productName,
    string productSKU,
    int PlannedQuantity,
    int? ProcessedQuantity,
    Ulid? sourceBinId,
    string? sourceBinCode,
    Ulid? destinationBinId,
    string? destinationBinCode
)
{
    public WarehouseOperationItemsDetailsResponse(WarehouseOperationItem woi) :
        this(woi.Id, woi.WarehouseOperationId, woi.ProductId, woi.Product!.Name, woi.Product.SKU,
            woi.PlannedQuantity, woi.ProcessedQuantity,
            woi.SourceBin != null ? woi.SourceBinId : null,
            woi.SourceBin != null ? woi.SourceBin.BinCode : null,
            woi.DestinationBin != null ? woi.DestinationBinId : null,
            woi.DestinationBin != null ? woi.DestinationBin.BinCode : null)
    { }
}