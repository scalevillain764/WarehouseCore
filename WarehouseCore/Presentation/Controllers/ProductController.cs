using Application.Products;
using Application.Products.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductController : BaseController
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    public async Task<IActionResult> AddProductAsync(
        [FromBody] CreateProductRequest request,
        CancellationToken token)
        => ProcessResult(await _productService.AddProductAsync(currentUserId, request, token));

    [HttpDelete("{productId}")]
    public async Task<IActionResult> RemoveProductAsync(
        Ulid productId,
        CancellationToken token)
        => ProcessResult(await _productService.RemoveProductAsync(currentUserId, productId, token));

    [HttpGet("{productId}")]
    public async Task<IActionResult> GetProductByIdAsync(
        Ulid productId,
        CancellationToken token)
        => ProcessResult(await _productService.GetProductByIdAsync(currentUserId, productId, token));

    [HttpGet]
    public async Task<IActionResult> GetProductsByQueryAsync(
        [FromQuery] GetProductsQueryRequest request,
        CancellationToken token)
        => ProcessResult(await _productService.GetProductsByQuery(currentUserId, request, token));
}