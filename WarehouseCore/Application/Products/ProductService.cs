using Domain.Models;
using Infrastructure.ResponsePatterns;
using Application.Products.Requests;
using Application.Products.Responses;
using Application.Features;
using Application.CompanyContext;
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

        var adminCheck = await _companyValidator.EnsureUserIsAdminAsync(context, userId, "Only admins and managers can add new products", token);
        var managerCheck = await _companyValidator.EnsureUserIsManagerAsync(context, userId, "Only admins and managers can add new products", token);

        if (!adminCheck.IsSuccess)
            return Result<ProductDetailsResponse>.Fail((ErrorType)adminCheck.ErrorType, adminCheck.ErrorMessage);

        if(!managerCheck.IsSuccess)
            return Result<ProductDetailsResponse>.Fail((ErrorType)managerCheck.ErrorType, managerCheck.ErrorMessage);

        var newProduct = new Product(request.Name, request.Description, request.Category, request.Brand, request.UnitOfMeasure, request.WeightPerUnit, request.Color);

        context.Products.Add(newProduct);

        await context.SaveChangesAsync(token);

        return Result<ProductDetailsResponse>.Success(new ProductDetailsResponse(newProduct));
    }
}