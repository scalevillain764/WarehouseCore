using Domain.Models;

namespace Application.WarehouseOperations.Requests;

public sealed record CreateWarehouseOperationRequest(
     WarehouseOperationType Type);