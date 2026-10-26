using Domain.Models;
namespace Application.WarehouseOperations.Responses;

public sealed record WarehouseOperationDetailsResponse(
    Ulid Id, 
    string? OperationNumber, 
    string Type, 
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    Ulid CreatedById,
    string UserName,
    string SecondName,
    string WorkerType)
{
    public WarehouseOperationDetailsResponse(WarehouseOperation wo) : this(wo.Id,
        wo.OperationNumber,
        wo.Type.ToString(),
        wo.Status.ToString(),
        wo.CreatedAt,
        wo.CompletedAt,
        wo.CreatedById,
        wo.CreatedBy!.FirstName,
        wo.CreatedBy.SecondName,
        wo.CreatedBy.Type.ToString())
    { }
}