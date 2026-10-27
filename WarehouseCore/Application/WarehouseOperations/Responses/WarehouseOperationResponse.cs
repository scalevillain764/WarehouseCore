using Domain.Models;
namespace Application.WarehouseOperations.Responses;

public sealed record WarehouseOperationResponse(Ulid Id, string OperationNumber, string Type, string Status)
{
    public WarehouseOperationResponse(WarehouseOperation wp) : this(wp.Id, wp.OperationNumber, wp.Type.ToString(), wp.Status.ToString()) { }
}