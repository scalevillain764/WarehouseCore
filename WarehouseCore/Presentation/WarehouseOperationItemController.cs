using Application.WarehouseOperationItems;
using Application.WarehouseOperationItems.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WarehouseOperationItemController : BaseController
{
    private readonly IWarehouseOperatiomItemService _warehouseOperationItemService;

    public WarehouseOperationItemController(IWarehouseOperatiomItemService warehouseOperationItemService)
    {
        _warehouseOperationItemService = warehouseOperationItemService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateWarehouseOperationItemAsync(
        [FromBody] CreateWarehouseOperationItemsRequest request,
        CancellationToken token)
        => ProcessResult(await _warehouseOperationItemService.CreateWarehouseOperationItemAsync(currentUserId, request, token));

    [HttpDelete("{warehouseOperationId}/{warehouseOperationItemId}")]
    public async Task<IActionResult> RemoveWarehouseOperationItemAsync(
        Ulid warehouseOperationId,
        Ulid warehouseOperationItemId,
        CancellationToken token)
        => ProcessResult(await _warehouseOperationItemService.RemoveWarehouseOperationItem(current