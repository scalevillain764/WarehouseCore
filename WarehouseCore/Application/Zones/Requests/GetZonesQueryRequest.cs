using Domain.Models;

namespace Application.Zones;
public sealed record GetZonesQueryRequest(
        List<WarehouseZoneType>? Types,
        List<Ulid>? Warehouses,
        bool IsActive,
        string? Name,
        int Page = 1,
        int PageSize = 1,
        int TotalCount = 1
    );