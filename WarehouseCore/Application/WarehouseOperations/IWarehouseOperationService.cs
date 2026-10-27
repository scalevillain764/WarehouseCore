using Application.WarehouseOperations.Requests;
using Application.WarehouseOperations.Responses;
using Domain.Models;
using Infrastructure.ResponsePatterns;
namespace Application.WarehouseOperations;
public interface IWarehouseOperationService
{
    Task<Result<WarehouseOperationDetailsResponse>> CreateWarehouseOperationAsync(Ulid userId, 
        CreateWarehouseOperationRequest request, CancellationToken token);

    Task<Result<WarehouseOperationDetailsResponse>> GetWarehouseOperationByIdAsync(Ulid userId, 
        Ulid warehouseOperationId, CancellationToken token);

    Task<Result<PagedResponse<WarehouseOperationResponse>>> GetWarehouseOperationsAsync(Ulid userId, 
        GetWarehouseOperationsRequest request, CancellationToken token);

    Task<Result<WarehouseOperationDetailsResponse>> CompleteWarehouseOperationAsync(Ulid userId, 
        Ulid warehouseOperationId, CancellationToken token);

    Task<Result<WarehouseOperationDetailsResponse>> CancelWarehouseOperationAsync(Ulid userId, 
        Ulid warehouseOperationId, CancellationToken token);
}