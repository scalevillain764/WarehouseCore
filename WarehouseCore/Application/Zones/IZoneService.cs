using Application.Zones.Requests;
using Application.Zones.Responses;
using Infrastructure.ResponsePatterns;
namespace Application.Zones;
public interface IZoneService
{
    Task<Result<PagedResponse<ZoneResponse>>> GetZonesAsync(Ulid userId, GetZonesQueryRequest request, CancellationToken token);
    Task<Result<ZoneDetailsResponse>> GetZoneAsync(Ulid userId, Ulid zoneId, CancellationToken token);
    Task<Result<ZoneDetailsResponse>> CreateZoneAsync(Ulid userId, CreateZoneRequest request, CancellationToken token);
    Task<Result<string>> RemoveZoneAsync(Ulid userId, Ulid zoneId, CancellationToken token);
    Task<Result<ZoneDetailsResponse>> ActivateZoneAsync(Ulid userId, Ulid zoneId, CancellationToken token);
}