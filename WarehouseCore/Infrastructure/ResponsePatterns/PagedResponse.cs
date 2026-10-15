namespace Infrastructure.ResponsePatterns
{
    public record PagedResponse<T>(
       IEnumerable<T> Items,
       int Page,
       int PageSize,
       int TotalCount
       );
}