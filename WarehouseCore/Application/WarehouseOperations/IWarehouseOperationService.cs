using Application.WarehouseOperations.Requests;
using Application.WarehouseOperations.Responses;
using Domain.Models;
using Infrastructure.ResponsePatterns;
namespace Application.WarehouseOperations;
public interface IWarehouseOperationService
{
    Task<Result<WarehouseOperationDetailsResponse>> CreateWarehouseOperationAsync(Ulid userId, CreateWarehouseOperationRequest request, CancellationToken token);
}