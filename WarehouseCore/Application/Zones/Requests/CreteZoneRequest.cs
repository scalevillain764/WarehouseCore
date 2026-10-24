using Domain.Models;

namespace Application.Zones.Requests;

public sealed record CreateZoneRequest(
    string Name,
    string? Description,
    WarehouseZoneType Type,
    Ulid WarehouseId);