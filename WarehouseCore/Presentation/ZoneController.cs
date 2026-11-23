using Application.Products.Requests;
using Application.Zones;
using Application.Zones.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseZone = Domain.Models.WarehouseZone;
namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ZoneController : BaseController
{
    private readonly IZoneService _zoneService;
    public ZoneController(IZoneService zoneService)
    {
        _zoneService = zoneService;
    }

    [HttpGet("{zoneId}")]
    public async Task<IActionResult> GetWarehouseZoneAsync([FromRoute] Ulid zoneId, CancellationToken token)
        => ProcessResult(await _zoneService.GetZoneAsync(currentUserId, zoneId, token));

    [HttpPost]
    public async Task<IActionResult> CreateZoneAsync([FromBody] CreateZoneRequest request, CancellationToken token)
        => ProcessResult(await _zoneService.CreateZoneAsync(currentUserId, request, token));

    [HttpPatch("remove/{zoneId}")]
    public async Task<IActionResult> RemoveZoneAsync([FromRoute] Ulid zoneId, CancellationToken token)
        => ProcessResult(await _zoneService.RemoveZoneAsync(currentUserId, zoneId, token));

    [HttpPatch("activate/{zoneId}")]
    public async Task<IActionResult> ActivateZoneAsync([FromRoute] Ulid zoneId, CancellationToken token)
        => ProcessResult(await _zoneService.ActivateZoneAsync(currentUserId, zoneId, token));

    [HttpGet("search")]
    public async Task<IActionResult> SearchAsync([FromQuery] GetZonesQueryRequest request, CancellationToken token)
        => ProcessResult(await _zoneService.GetZonesAsync(currentUserId, request, token));

}