using Domain.Models;

namespace Application.Bins.Requests
{
    public record AddSectionRequest(
        Ulid userId,
        Ulid zoneId, 
        int rowNumber,
        int levels,
        BinTypeByCapacity type)
    { }
}