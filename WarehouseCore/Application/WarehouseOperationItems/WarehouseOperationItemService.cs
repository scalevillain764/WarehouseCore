using Application.CompanyContext;
using Application.Features;
using Application.WarehouseOperationItems.Responses;
using Application.WarehouseOperations.Responses;
using Domain.Models;
using Infrastructure.ResponsePatterns;
using Microsoft.EntityFrameworkCore;
namespace Application.WarehouseOperationItems;
public class WarehouseOperationItemService : IWarehouseOperatiomItemService
{
    private readonly IWmsDbContextFactory _wmsFactory;
    private readonly ICompanyAcessValidator _companyValidator;
    private readonly ICurrentCompanyContext _currentCompanyContext;

    public WarehouseOperationItemService(IWmsDbContextFactory wmsFactory, ICompanyAcessValidator companyValidator, ICurrentCompanyContext currentCompanyContext)
    {
        _wmsFactory = wmsFactory;
        _companyValidator = companyValidator;
        _currentCompanyContext = currentCompanyContext;
    }

    public async Task<Result<WarehouseOperationItemsDetailsResponse>> CreateWarehouseOperationItemAsync(Ulid userId,
        CreateWarehouseOperationItemsRequest request, CancellationToken token)
    {
        await using var context = await _wmsFactory
            .GetContextAsync(_currentCompanyContext.CompanyId, token);

        var adminAndManagerCheck = await _companyValidator.EnsureUserHasAnyRoleAsync(context, userId, "Only admins and managers can create warehouse operation",
            new List<UserType> { UserType.Admin, UserType.Manager }, token);

        if (!adminAndManagerCheck.IsSuccess)
            return Result<WarehouseOperationItemsDetailsResponse>.Fail((ErrorType)adminAndManagerCheck.ErrorType, adminAndManagerCheck.ErrorMessage);

        var warehouseOperation = await context.WarehouseOperations
            .FindAsync(request.warehouseOperationId, token);

        if (warehouseOperation is null)
            return Result<WarehouseOperationItemsDetailsResponse>.Fail(ErrorType.NotFound, "Warehouse operation not found");

        var product = await context.Products
            .FindAsync(request.productId, token);

        if (product is null)
            return Result<WarehouseOperationItemsDetailsResponse>.Fail(ErrorType.NotFound, "Product not found");

        var newWarehouseOperationItem = new WarehouseOperationItem()
    }
}