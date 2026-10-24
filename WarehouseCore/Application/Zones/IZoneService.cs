using Application.Zones.Requests;
using Application.Zones.Responses;
using Infrastructure.ResponsePatterns;
namespace Application.Zones;
public interface IZoneService
{
    Task<Result<ZoneResponse>> GetZoneAsync(Ulid userId, Ulid zoneId, CancellationToken token);
    Task<Result<ZoneResponse>> CreateZoneAsync(Ulid userId, CreateZoneRequest request, CancellationToken token);
    Task<Result<string>> RemoveZoneAsync(Ulid userId, Ulid zoneId, CancellationToken token);
    Task<Result<ZoneResponse>> ActivateZoneAsync(Ulid userId, Ulid zoneId, CancellationToken token);
}