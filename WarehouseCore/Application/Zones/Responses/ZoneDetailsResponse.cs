namespace Application.Zones.Responses;

using Domain.Models;

public sealed record ZoneDetailsResponse(Ulid Id,
    string Name,
    string? Description,
    string Code,
    string Type,
    bool IsActive,
    string WarehouseName,
    int BinsCount)
{
    public ZoneDetailsResponse(WarehouseZone z) : this(z.Id, z.Name, z.Description, z.Code, z.Type.ToString(), z.IsActive, z.Warehouse!.Name, z.Bins.Count()) { }
}