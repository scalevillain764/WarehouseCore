using Application.WarehouseOperationItems.Responses;
using Infrastructure.ResponsePatterns;
using Application.WarehouseOperationItems.Requests;
namespace Application.WarehouseOperationItems;
public interface IWarehouseOperatiomItemService
{
    Task<Result<WarehouseOperationItemsDetailsResponse>> CreateWarehouseOperationItemAsync(Ulid userId,
        CreateWarehouseOperationItemsRequest request, CancellationToken token);

    Task<Result<string>> RemoveWarehouseOperationItem(Ulid userId, Ulid warehouseOperationId,
        Ulid warehouseOperationItemId, CancellationToken token);
}