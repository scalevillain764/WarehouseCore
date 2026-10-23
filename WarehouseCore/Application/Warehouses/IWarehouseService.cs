using Application.Warehouses.Requests;
using Application.Warehouses.Responses;

namespace Application.Warehouses;
public interface IWarehouseService
{
    Task<CreateWarehouseResponse> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken token);
}