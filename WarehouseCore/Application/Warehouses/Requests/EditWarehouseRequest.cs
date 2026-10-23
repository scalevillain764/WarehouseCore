using Domain.Models;

namespace Application.Warehouses.Requests;

public sealed record EditWarehouseRequest(
        Ulid warehouseId,
        string WarehouseName,
        string Country,
        string City,
        string Street,
        string NumberBuilding);