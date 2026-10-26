using Application.CompanyContext;
using Application.Features;
using Application.WarehouseOperations.Requests;
using Application.WarehouseOperations.Responses;
using Domain.Models;
using Infrastructure.ResponsePatterns;

namespace Application.WarehouseOperations;
public class WarehouseOperationService : IWarehouseOperationService
{
    private readonly IWmsDbContextFactory _wmsFactory;
    private readonly ICurrentCompanyContext _currentCompany;
    private readonly ICompanyAcessValidator _companyValidator;
    public WarehouseOperationService(IWmsDbContextFactory wmsFactory, ICurrentCompanyContext currentCompany, ICompanyAcessValidator companyValidator)
    {
        _wmsFactory = wmsFactory;
        _currentCompany = currentCompany;
        _companyValidator = companyValidator;
    }
    public async Task<Result<WarehouseOperationDetailsResponse>> CreateWarehouseOperationAsync(Ulid userId, 
        CreateWarehouseOperationRequest request, CancellationToken token)
    {
        await using var context = await _wmsFactory
            .GetContextAsync(_currentCompany.CompanyId, token);

        var adminAndManagerCheck = await _companyValidator.EnsureUserHasAnyRoleAsync(context, userId, "Only admins and managers can create warehouse operation",
            new List<UserType> { UserType.Admin, UserType.Manager }, token);

        if (!adminAndManagerCheck.IsSuccess)
            return Result<WarehouseOperationDetailsResponse>.Fail((ErrorType)adminAndManagerCheck.ErrorType, adminAndManagerCheck.ErrorMessage);

        var warehouseOperation = new WarehouseOperation(request.Type, userId);

        context.WarehouseOperations.Add(warehouseOperation);

        await context.SaveChangesAsync(token);

        return Result<WarehouseOperationDetailsResponse>.Success(new WarehouseOperationDetailsResponse(warehouseOperation));
    }
}