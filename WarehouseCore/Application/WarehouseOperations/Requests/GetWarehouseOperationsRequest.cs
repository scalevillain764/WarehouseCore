using Domain.Models;
using System.Runtime.Intrinsics.X86;

namespace Application.WarehouseOperations.Requests;

public sealed record GetWarehouseOperationsRequest(
    string? OperationNumber,
    List<WarehouseOperationType>? Types,
    List<WarehouseOperationStatus>? Statuses,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    Ulid? CreatedById,
    string? FirstName,
    string? SecondName,
    UserType? WorkerType,
    int Page = 1,
    int PageSize = 1
    );