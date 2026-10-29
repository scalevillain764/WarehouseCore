using Domain.Models;
using Infrastructure.ResponsePatterns;
using Application.Products.Requests;
using Application.Products.Responses;
using Application.Features;
using Application.CompanyContext;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
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

        product.IsActive = false;

        await context.SaveChangesAsync(token);

        return Result<string>.Success("OK");
    }

    public async Task<Result<ProductDetailsResponse>> GetProductByIdAsync(Ulid userId, Ulid productId, CancellationToken token)
    {
        await using var context = await _wmsFactory
          .GetContextAsync(_companyContext.CompanyId, token);

        var product = await context.Products
            .AsNoTracking()
            .Select(x => new ProductDetailsResponse(
                x.Id,
                x.Name,
                x.Description,
                x.Category.ToString(),
                x.Brand.ToString(),
                x.IsActive,
                x.UnitOfMeasure.ToString(),
                x.WeightPerUnit,
                x.Color,
                x.SKU
             ))
            .FirstOrDefaultAsync(x => x.Id == userId, token);

        if (product is null)
            return Result<ProductDetailsResponse>.Fail(ErrorType.NotFound, "Product not found");

        return Result<ProductDetailsResponse>.Success(product);
    }

    public async Task<Result<PagedResponse<ProductResponse>>> GetProductsByQuery(Ulid userId, GetProductsQueryRequest request, CancellationToken token)
    {
        await using var context = await _wmsFactory
            .GetContextAsync(_companyContext.CompanyId, token);

        if (request.Page < 1 || request.PageSize < 1)
            return Result<PagedResponse<ProductResponse>>.Fail(ErrorType.Validation, "Validation error");

        var products_queryable = context.Products
            .AsQueryable();

        if(request.Name is not null)
        {
            var name_words = request.Name.ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach(var word in name_words)
            {
                products_queryable = products_queryable
                    .Where(x => x.Name.ToLower().Contains(word) || x.Name.ToLower() == word);
            }
        }

        if(request.Description is not null)
        {
            var description_words = request.Description.ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
            foreach(var w in description_words)
            {
                products_queryable.Where(x => x.Description != null && (x.Description.ToLower().Contains(w)
                || x.Description.ToLower() == w));
            }
        }

        if(request.Categories is not null && request.Categories.Any())
            products_queryable = products_queryable
                .Where(x => request.Categories.Contains(x.Category));

        if (request.Brand is not null)
            products_queryable = products_queryable
                .Where(x => x.Brand == request.Brand);

        if (request.UnitsOfMeasure is not null && request.UnitsOfMeasure.Any())
            products_queryable = products_queryable
                .Where(x => request.UnitsOfMeasure.Contains(x.UnitOfMeasure));

        if(request.WeightPerUnit is not null)
            products_queryable = products_queryable
                .Where(x => x.WeightPerUnit == request.WeightPerUnit);

        if(!request.IsActive)
        {
            var adminAndManagerCheck = await _companyValidator.EnsureUserHasAnyRoleAsync(context, userId,
                "Only admins and managers can see inactive products", new List<UserType> { UserType.Manager, UserType.Admin }, token);

            if (!adminAndManagerCheck.IsSuccess)
                return Result<PagedResponse<ProductResponse>>
                    .Fail((ErrorType)adminAndManagerCheck.ErrorType, adminAndManagerCheck.ErrorMessage);

            products_queryable = products_queryable
                    .IgnoreQueryFilters()
                        .Where(x => !x.IsActive);        
        }

        if (request.SKU is not null)
            products_queryable = products_queryable
                .Where(x => x.SKU == request.SKU);

        if (request.Color is not null)
            products_queryable = products_queryable
                .Where(x => x.Color != null && x.Color == request.Color);

        int totalCount = await products_queryable
            .CountAsync(token);

        var rez = await products_queryable
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ProductResponse(
                x.Id,
                x.Name,
                x.Category.ToString(),
                x.Brand,
                x.SKU
            ))
            .ToListAsync(token);

        return Result<PagedResponse<ProductResponse>>.Success(new PagedResponse<ProductResponse>(rez, request.Page, request.PageSize, totalCount));
    }
}