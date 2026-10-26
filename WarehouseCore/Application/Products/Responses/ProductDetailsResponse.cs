using Domain.Models;
namespace Application.Products.Responses;
public sealed record ProductDetailsResponse(Ulid Id,
    string Name,
    string? Description,
    string Category,
    string Brand,
    bool IsActive,
    string UnitOfMeasure,
    decimal WeightPerUnit,
    string? Color,
    string SKU)
{
    public ProductDetailsResponse(Product p) : 
        this(p.Id, p.Name, p.Description, p.Category.ToString(), 
            p.Brand, p.IsActive, p.UnitOfMeasure.ToString(), 
            p.WeightPerUnit, p.Color, p.SKU)
    { }
}