using Domain.Models;
namespace Application.Products.Requests;
public sealed record CreateProductRequest(
    string Name,
    string? Description,
    ProductCategory Category,
    string Brand,
    UnitOfMeasure UnitOfMeasure,
    decimal WeightPerUnit,
    string? Color
    );