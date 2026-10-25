namespace Application.Zones.Responses;
using Domain.Models;

public sealed record ZoneResponse(Ulid Id, 
    string Name,
    string? Description,
    string Code,
    string Type)
{
    public ZoneResponse(WarehouseZone z) : this(z.Id, z.Name, z.Description, z.Code, z.Type.ToString()) { }
}