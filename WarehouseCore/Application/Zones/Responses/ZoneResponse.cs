namespace Application.Zones.Responses;
using Domain.Models;

public sealed record ZoneResponse(Ulid Id, 
    string Name,
    string Code,
    string Type)
{
    public ZoneResponse(WarehouseZone z) : this(z.Id, z.Name, z.Code, z.Type.ToString()) { }
}