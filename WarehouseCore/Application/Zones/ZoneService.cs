using Application.CompanyContext;
using Application.Features;
using Application.Zones.Requests;
using Application.Zones.Responses;
using Domain.Models;
using Infrastructure;
using Infrastructure.ResponsePatterns;
using Microsoft.EntityFrameworkCore;

namespace Application.Zones;
public class ZoneService : IZoneService
{
    private readonly IWmsDbContextFactory _wmsFactory;
    private readonly ICurrentCompanyContext _companyContext;
    private readonly ICompanyAcessValidator _companyValidator;
    public ZoneService(IWmsDbContextFactory wmsFactory,
        ICurrentCompanyContext companyContext, 
        ICompanyAcessValidator companyValidator)
    {
        _wmsFactory = wmsFactory;
        _companyContext = companyContext;
        _companyValidator = companyValidator;
    }
    
    public async Task<Result<ZoneResponse>> GetZoneAsync(Ulid userId, Ulid warehouseId, Ulid zoneId, CancellationToken token)
    {
        var context = await _wmsFactory
            .GetContextAsync(_companyContext.CompanyId, token);

        if (!await _companyValidator.EnsureUserExistsAsync(context, userId, token))
            return Result<ZoneResponse>.Fail(ErrorType.NotFound, "User doesn't belong to company");

        var zone = await context.Zones
            .Where(x => x.WarehouseId == warehouseId)
            .FirstOrDefaultAsync(x => x.Id == zoneId);

        return zone is null
            ? Result<ZoneResponse>.Fail(ErrorType.NotFound, "Zone not found")
            : Result<ZoneResponse>.Success(new ZoneResponse(zone));
    }

    public async Task<Result<ZoneResponse>> CreateZoneAsync(Ulid userId, CreateZoneRequest request, CancellationToken token)
    {
        var context = await _wmsFactory
            .GetContextAsync(_companyContext.CompanyId, token);

        var adminCheckRequest = await _companyValidator
            .EnsureUserIsAdminAsync(context, userId, "Only admins can create zones", token);

        if (!adminCheckRequest.IsSuccess)
            return Result<ZoneResponse>.Fail((ErrorType)adminCheckRequest.ErrorType, adminCheckRequest.ErrorMessage);

        var newZone = new WarehouseZone(request.Name, request.Description, request.WarehouseId, request.Type);

    }

}