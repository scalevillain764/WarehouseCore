using Application.Products.Requests;
using Infrastructure.ResponsePatterns;
using Application.Products.Responses;
namespace Application.Products;
public interface IProductService
{
    Task<Result<ProductDetailsResponse>> AddProductAsync(Ulid userId, CreateProductRequest request, CancellationToken token);
    Task<Result<string>> RemoveProductAsync(Ulid userId, Ulid productId, CancellationToken token);
}