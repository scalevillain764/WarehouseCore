using Application.WarehouseOperationItems.Responses;
using Infrastructure.ResponsePatterns;
namespace Application.WarehouseOperationItems;
public interface IWarehouseOperatiomItemService
{
    Task<Result<WarehouseOperationItemsDetailsResponse>> CreateWarehouseOperationItemAsync(Ulid userId,
        CreateWarehouseOperationItemsRequest request, CancellationToken token);
}