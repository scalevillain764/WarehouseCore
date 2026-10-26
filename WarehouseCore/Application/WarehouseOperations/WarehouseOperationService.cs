using Application.CompanyContext;
using Application.Features;
using Application.WarehouseOperations.Requests;
using Application.WarehouseOperations.Responses;
using Infrastructure.ResponsePatterns;

namespace Application.WarehouseOperations;
public class WarehouseOperationService : IWarehouseOperationService
{
    private readonly IWmsDbContextFactory _wmsFactory;
    private readonly ICurrentCompanyContext _currentCompany;
    public WarehouseOperationService(IWmsDbContextFactory wmsFactory, ICurrentCompanyContext currentCompany)
    {
        _wmsFactory = wmsFactory;
        _currentCompany = currentCompany;
    }
    public async Task<Result<WarehouseOperationDetailsResponse>> CreateWarehouseOperation(Ulid userId, 
        CreateWarehouseOperationRequest request, CancellationToken token)
    {
        await using var context = await _wmsFactory
            .GetContextAsync(_currentCompany.CompanyId, token);
    }
}