using Domain.Models;

namespace Application.Zones;
public sealed record GetZonesQueryRequest(
        List<WarehouseZoneType>? Types = null,
        List<Ulid>? Warehouses = null,
        bool IsActive = true,
        string? Name = null,
        int Page = 1,
        int PageSize = 10,
        int TotalCount = 1
    );