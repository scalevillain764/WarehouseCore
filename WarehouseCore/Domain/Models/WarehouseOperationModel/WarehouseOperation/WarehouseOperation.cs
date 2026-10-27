namespace Domain.Models
{
    public class WarehouseOperation
    {
        public Ulid Id { get; init; }
        public string OperationNumber { get; init; }
        public WarehouseOperationType Type { get; init; }
        public WarehouseOperationStatus Status { get; set; }
        public DateTime CreatedAt { get; init; }
        public DateTime? CompletedAt { get; set; } = null;
        public Ulid CreatedById { get; init; }
        public User? CreatedBy { get; set; } = null!;
        public ICollection<WarehouseOperationItem> Items { get; set; } = [];

        private WarehouseOperation()
        {
            Items = [];
        }

        private string GetTypePrefix(WarehouseOperationType type)
            => type switch
            {
                WarehouseOperationType.Transfer => "TRF",
                WarehouseOperationType.Putaway => "PTA",
                WarehouseOperationType.Picking => "PCK",
                WarehouseOperationType.Receiving => "RCV",
                WarehouseOperationType.Shipping => "SHP",
                _ => throw new InvalidDataException("Invalid warehouse operation type")

            };

        public WarehouseOperation(WarehouseOperationType type, Ulid createdById)
        {
            Id = Ulid.NewUlid();
            Type = type;
            Status = WarehouseOperationStatus.Created;
            CreatedAt = DateTime.UtcNow;
            CreatedById = createdById;
            OperationNumber = $"{GetTypePrefix(type)}-{Id}";
        }
    }
}