using Domain.Models;

namespace Application.Warehouses.Requests;

public sealed record CreateWarehouseRequest(Ulid CompanyId,
        string WarehouseName,
        string Country,
        string City,
        string Street,
        string NumberBuilding);