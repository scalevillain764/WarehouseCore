using Domain.Models;
namespace Application.Warehouses.Responses;
public sealed record CreateWarehouseResponse(Ulid Id,
      string WarehouseName,
      string Country,
      string City,
      string Street,
      string NumberBuilding)
{
    public CreateWarehouseResponse(Warehouse w) : 
        this(w.Id, w.Name, w.Address.Country, w.Address.City, w.Address.Street, w.Address.NumberBuilding)
    { }
}