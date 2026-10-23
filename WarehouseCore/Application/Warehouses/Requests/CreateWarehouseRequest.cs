using Domain.Models;

namespace Application.Warehouses.Requests;

public sealed record CreateWarehouseRequest(
        string WarehouseName,
        string Country,
        string City,
        string Street,
        string NumberBuilding);