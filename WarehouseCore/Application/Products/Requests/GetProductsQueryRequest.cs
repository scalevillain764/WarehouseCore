using Domain.Models;

namespace Application.Products.Requests;

public sealed record GetProductsQueryRequest(
    string? Name,
    string? Description,
    List<ProductCategory>? Categories,
    string? Brand,
    List<UnitOfMeasure>? UnitsOfMeasure,
    decimal? WeightPerUnit,
    bool IsActive,
    string? SKU,
    string? Color,
    int Page = 1,
    int PageSize = 1
    );