using Domain.Models;
using Infrastructure.ResponsePatterns;
using Application.Products.Requests;
using Application.Products.Responses;
using Application.Features;
using Application.CompanyContext;
using Microsoft.EntityFrameworkCore;
namespace Application.Products;
public class ProductService : IProductService
{
    private readonly IWmsDbContextFactory _wmsFactory;
    private readonly ICurrentCompanyContext _companyContext;
    private readonly ICompanyAcessValidator _companyValidator;
    public ProductService(IWmsDbContextFactory wmsFactory, ICurrentCompanyContext companyContext, ICompanyAcessValidator companyValidator)
    {
        _wmsFactory = wmsFactory;
        _companyContext = companyContext;
        _companyValidator = companyValidator;
    }
    public async Task<Result<ProductDetailsResponse>> AddProductAsync(Ulid userId, CreateProductRequest request, CancellationToken token)
    {
        await using var context = await _wmsFactory
            .GetContextAsync(_companyContext.CompanyId, token);

        var adminOrManagerCheck = await _companyValidator
            .EnsureUserHasAnyRoleAsync(context, userId, "Only admins and managers can add new products",
            new List<UserType> { UserType.Admin, UserType.Manager }, token);
    
        if(!adminOrManagerCheck.IsSuccess)
            return Result<ProductDetailsResponse>.Fail((ErrorType)adminOrManagerCheck.ErrorType, adminOrManagerCheck.ErrorMessage);

        var newProduct = new Product(request.Name, request.Description, request.Category, request.Brand, request.UnitOfMeasure, request.WeightPerUnit, request.Color);

        context.Products.Add(newProduct);

        await context.SaveChangesAsync(token);

        return Result<ProductDetailsResponse>.Success(new ProductDetailsResponse(newProduct));
    }

    public async Task<Result<string>> RemoveProductAsync(Ulid userId, Ulid productId, CancellationToken token)
    {
        await using var context = await _wmsFactory
           .GetContextAsync(_companyContext.CompanyId, token);

        var adminOrManagerCheck = await _companyValidator
             .EnsureUserHasAnyRoleAsync(context, userId, "Only admins and managers can remove products",
             new List<UserType> { UserType.Admin, UserType.Manager }, token);

        if (!adminOrManagerCheck.IsSuccess)
            return Result<string>.Fail((ErrorType)adminOrManagerCheck.ErrorType, adminOrManagerCheck.ErrorMessage);

        var product = await context.Products
            .Include(x => x.Stocks)
            .FirstOrDefaultAsync(x => x.Id == productId, token);

        if (product is null)
            return Result<string>.Fail(ErrorType.NotFound, "Product not found");

        if (product.Stocks.Any())
            return Result<string>.Fail(ErrorType.Conflict, "Bins still contain the product. Please remove it from all bins");

        context.Products.Remove(product);

        await context.SaveChangesAsync(token);

        return Result<string>.Success("OK");
    }
}