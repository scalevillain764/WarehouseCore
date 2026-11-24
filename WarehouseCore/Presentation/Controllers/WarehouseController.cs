using Application.Warehouses;
using Application.Warehouses.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WarehouseController : BaseController
{
    private readonly IWarehouseService _warehouseService;

    public WarehouseController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateWarehouseAsync(
        [FromBody] CreateWarehouseRequest request,
        CancellationToken token)
        => ProcessResult(await _warehouseService.CreateWarehouseAsync(currentUserId, request, token));

    [HttpDelete("{warehouseId}")]
    public async Task<IActionResult> RemoveWarehouseAsync(
        Ulid warehouseId,
        CancellationToken token)
        => ProcessResult(await _warehouseService.RemoveWarehouseAsync(currentUserId, warehouseId, token));

    [HttpPatch]
    public async Task<IActionResult> EditWarehouseAsync(
        [FromBody] EditWarehouseRequest request,
        CancellationToken token)
        => ProcessResult(await _warehouseService.EditWarehouseAsync(currentUserId, request, token));

    [HttpGet("{warehouseId}")]
    public async Task<IActionResult> GetWarehouseAsync(
        Ulid warehouseId,
        CancellationToken token)
        => ProcessResult(await _warehouseService.GetWarehouseAsync(currentUserId, warehouseId, token));

    [HttpPatch("{warehouseId}/activate")]
    public async Task<IActionResult> ActivateWarehouseAsync(
        Ulid warehouseId,
        CancellationToken token)
        => ProcessResult(await _warehouseService.ActivateWarehouseAsync(currentUserId, warehouseId, token));
}