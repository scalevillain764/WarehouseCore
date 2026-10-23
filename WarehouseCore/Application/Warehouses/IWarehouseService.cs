using Application.Warehouses.Requests;
using Application.Warehouses.Responses;
using Infrastructure.ResponsePatterns;
using Microsoft.IdentityModel.Tokens;

namespace Application.Warehouses;
public interface IWarehouseService
{
    Task<Result<WarehouseResponse>> CreateWarehouseAsync(Ulid userId, CreateWarehouseRequest request, CancellationToken token);
    Task<Result<string>> RemoveWarehouseAsync(Ulid userId, Ulid warehouseId, CancellationToken token);
    Task<Result<WarehouseResponse>> EditWarehousAsync(Ulid userId, EditWarehouseRequest request, CancellationToken token);
}