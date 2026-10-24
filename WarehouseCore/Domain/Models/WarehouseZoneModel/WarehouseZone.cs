using System.Diagnostics.CodeAnalysis;

namespace Domain.Models
{
    public class WarehouseZone
    {
        public Ulid Id { get; init; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required string Code { get; init; }
        public WarehouseZoneType Type { get; set; }
        public bool IsActive { get; set; }

        // warehouse
        public Ulid WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; } = null!;

        // bin
        public IEnumerable<Bin> Bins { get; set; } = [];

        private string BuildCode()
        {
            string idString = Id.ToString();

            string idPart = idString.Replace(" ", "")
                .Substring(idString.Length - 5).ToUpper();

            string prefix = Type switch
            {
                WarehouseZoneType.Sorting => "SRT",
                WarehouseZoneType.Receiving => "RCV",
                WarehouseZoneType.Picking => "PCK",
                WarehouseZoneType.Quarantine => "QRN",
                WarehouseZoneType.Shipping => "SHP",
                WarehouseZoneType.BulkStorage => "BLK",
                _ => "ZN"
            };

            return $"{prefix}-{idPart}";
        }

        [SetsRequiredMembers]
        public WarehouseZone(string name, string? description, Ulid warehouseId, WarehouseZoneType type)
        {
            Id = Ulid.NewUlid();
            Name = name;
            Description = description;
            WarehouseId = warehouseId;
            Type = type;
            Code = BuildCode();
            IsActive = true;
        }
    }
}