using System.Diagnostics.CodeAnalysis;

namespace Domain.Models
{
    public class Bin
    {
        public Ulid Id { get; init; }
        public required string BinCode { get; init; }
        public Ulid WarehouseZoneId { get; set; }
        public WarehouseZone? Zone { get; set; } = null!;
        public required int Row { get; init; }
        public required int Section { get; init; }
        public required int Level { get; init; }
        public bool IsOccupied { get; set; }

        [SetsRequiredMembers]
        public Bin (string binCode, int row, int section, int level, Ulid warehouseZoneId)
        {
            Id = Ulid.NewUlid();
            BinCode = binCode;
            WarehouseZoneId = warehouseZoneId;
            IsOccupied = false;
            Row = row;
            Section = section;
            Level = level;
        }
    }
}