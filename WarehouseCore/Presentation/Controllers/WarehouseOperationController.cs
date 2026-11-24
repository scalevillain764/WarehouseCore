using Application.WarehouseOperations;
using Application.WarehouseOperations.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WarehouseOperationController : BaseController
{
    private readonly IWarehouseOperationService _warehouseOperationService;

    public WarehouseOperationController(IWarehouseOperationService warehouseOperationService)
    {
        _warehouseOperationService = warehouseOperationService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateWarehouseOperationAsync(
        [FromQuery] Ulid assignedToId,
        [FromBody] CreateWarehouseOperationRequest request,
        CancellationToken token)
        => ProcessResult(await _warehouseOperationService.CreateWarehouseOperationAsync(currentUserId, assignedToId, request, token));

    [HttpGet("{warehouseOperationId}")]
    public async Task<IActionResult> GetWarehouseOperationByIdAsync(
        Ulid warehouseOperationId,
        CancellationToken token)
        => ProcessResult(await _warehouseOperationService.GetWarehouseOperationByIdAsync(currentUserId, warehouseOperationId, token));

    [HttpGet]
    public async Task<IActionResult> GetWarehouseOperationsAsync(
        [FromQuery] GetWarehouseOperationsRequest request,
        CancellationToken token)
        => ProcessResult(await _warehouseOperationService.GetWarehouseOperationsAsync(currentUserId, request, token));

    [HttpPatch("{warehouseOperationId}/complete")]
    public async Task<IActionResult> CompleteWarehouseOperationAsync(
        Ulid warehouseOperationId,
        CancellationToken token)
        => ProcessResult(await _warehouseOperationService.CompleteWarehouseOperationAsync(currentUserId, warehouseOperationId, token));

    [HttpPatch("{warehouseOperationId}/cancel")]
    public async Task<IActionResult> CancelWarehouseOperationAsync(
        Ulid warehouseOperationId,
        CancellationToken token)
        => ProcessResult(await _warehouseOperationService.CancelWarehouseOperationAsync(currentUserId, warehouseOperationId, token));
}