using Domain.Models;

namespace Application.Bins.Requests
{
    public record AddRowRequest(
        Ulid userId,
        Ulid zoneId,
        int sections,
        int levels,
        BinTypeByCapacity type)
    { }
}