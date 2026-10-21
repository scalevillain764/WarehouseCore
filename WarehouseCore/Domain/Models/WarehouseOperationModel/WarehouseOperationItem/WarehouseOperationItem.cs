namespace Domain.Models
{
    public class WarehouseOperationItem
    {
        public Ulid Id { get; init; }
        public Ulid WarehouseOperationId { get; init; }
        public WarehouseOperation? WarehouseOperation { get; set; } = null;
        public Ulid ProductId { get; init; }
        public Product? Product { get; set; } = null;
        public int PlannedQuantity { get; set; }
        public int? ProcessedQuantity { get; set; } = null;
        public Ulid? SourceBinId { get; set; } = null;
        public Bin? SourceBin { get; set; } = null;
        public Ulid? DestinationBinId { get; set; } = null;
        public Bin? DestinationBin { get; set; } = null;
        private WarehouseOperationItem() { }
        public WarehouseOperationItem(Ulid warehouseOperationId, Ulid productId, int plannedQuantity, Ulid? sourceBinId, Ulid? destinationBinId)
        {
            Id = Ulid.NewUlid();
            WarehouseOperationId = warehouseOperationId;
            ProductId = productId;
            PlannedQuantity = plannedQuantity;
            SourceBinId = sourceBinId;
            DestinationBinId = destinationBinId;
        }
    }
}