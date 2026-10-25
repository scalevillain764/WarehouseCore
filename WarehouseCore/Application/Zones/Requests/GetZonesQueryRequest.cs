using Domain.Models;

namespace Application.Zones;
public sealed record GetZonesQueryRequest(
        List<WarehouseZoneType> Types,
        List<Ulid> Warehouses,
        string Name
    );