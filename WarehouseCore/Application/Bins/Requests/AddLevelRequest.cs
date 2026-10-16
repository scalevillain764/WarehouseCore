using Domain.Models;

namespace Application.Bins.Requests
{
    public record AddLevelRequest(
        Ulid userId,
        Ulid zoneId,
        int rowNumber, 
        int sectionNumber, 
        int levels,
        BinTypeByCapacity type)
    { }
}