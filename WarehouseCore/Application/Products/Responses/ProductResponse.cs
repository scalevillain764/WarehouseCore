using Domain.Models;
namespace Application.Products.Responses;

public sealed record ProductResponse(Ulid Id,
    string Name,
    string Category,
    string Brand,
    string SKU);