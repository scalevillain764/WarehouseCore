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
    
    public async Task<Result<ZoneDetailsResponse>> GetZoneAsync(Ulid userId, Ulid zoneId, CancellationToken token)
    {
        var context = await _wmsFactory
            .GetContextAsync(_companyContext.CompanyId, token);

        if (!await _companyValidator.EnsureUserExistsAsync(context, userId, token))
            return Result<ZoneDetailsResponse>.Fail(ErrorType.NotFound, "User doesn't belong to company");

        var zone = await context.Zones
            .AsNoTracking()
            .Select(x => new ZoneDetailsResponse
                (
                    x.Id,
                    x.Name,
                    x.Description,
                    x.Code,
                    x.Type.ToString(),
                    x.IsActive,
                    x.Warehouse.Name,
                    x.Bins.Count()
                ))
            .FirstOrDefaultAsync(x => x.Id == zoneId, token);

        return zone is null
            ? Result<ZoneDetailsResponse>.Fail(ErrorType.NotFound, "Zone not found")
            : Result<ZoneDetailsResponse>.Success(zone);
    }

    public async Task<Result<ZoneDetailsResponse>> CreateZoneAsync(Ulid userId, CreateZoneRequest request, CancellationToken token)
    {
        var context = await _wmsFactory
            .GetContextAsync(_companyContext.CompanyId, token);

        var adminCheckRequest = await _companyValidator
            .EnsureUserIsAdminAsync(context, userId, "Only admins can create zones", token);

        if (!adminCheckRequest.IsSuccess)
            return Result<ZoneDetailsResponse>.Fail((ErrorType)adminCheckRequest.ErrorType, adminCheckRequest.ErrorMessage);

        var newZone = new WarehouseZone(request.Name, request.Description, request.WarehouseId, request.Type);

        context.Zones.Add(newZone);

        await context.SaveChangesAsync(token);

        await context.Entry(newZone).Reference(x => x.Warehouse).LoadAsync(token);

        return Result<ZoneDetailsResponse>.Success(new ZoneDetailsResponse(newZone));
    }

    public async Task<Result<string>> RemoveZoneAsync(Ulid userId, Ulid zoneId, CancellationToken token)
    {
        var context = await _wmsFactory
            .GetContextAsync(_companyContext.CompanyId, token);

        var adminCheckRequest = await _companyValidator
            .EnsureUserIsAdminAsync(context, userId, "Only admins can remove zones", token);

        if (!adminCheckRequest.IsSuccess)
            return Result<string>.Fail((ErrorType)adminCheckRequest.ErrorType, adminCheckRequest.ErrorMessage);

        var zone = await context.Zones
            .Include(x => x.Bins)
            .FirstOrDefaultAsync(x => x.Id == zoneId, token);

        if (zone is null)
            return Result<string>.Fail(ErrorType.NotFound, "Zone not found");

        if (zone.Bins.Any(b => b.IsActive))
            return Result<string>.Fail(ErrorType.Conflict, "Zone containts active bins");

        zone.IsActive = false;

        await context.SaveChangesAsync(token);

        return Result<string>.Success("OK");
    }

    public async Task<Result<ZoneDetailsResponse>> ActivateZoneAsync(Ulid userId, Ulid zoneId, CancellationToken token)
    {
        var context = await _wmsFactory
           .GetContextAsync(_companyContext.CompanyId, token);

        var adminCheckRequest = await _companyValidator
            .EnsureUserIsAdminAsync(context, userId, "Only admins can activate zones", token);

        if (!adminCheckRequest.IsSuccess)
            return Result<ZoneDetailsResponse>.Fail((ErrorType)adminCheckRequest.ErrorType, adminCheckRequest.ErrorMessage);

        var zone = await context.Zones
            .IgnoreQueryFilters()
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == zoneId, token);

        if (zone is null)
            return Result<ZoneDetailsResponse>.Fail(ErrorType.NotFound, "Zone not found");

        zone.IsActive = true;

        await context.SaveChangesAsync(token);

        return Result<ZoneDetailsResponse>.Success(new ZoneDetailsResponse(zone));
    }

    public async Task<Result<PagedResponse<ZoneResponse>>> GetActiveZonesByCompany(Ulid userId, CancellationToken token)
    {
        var context = await _wmsFactory
            .GetContextAsync(userId, token);

        if (!await _companyValidator.EnsureUserExistsAsync(context, userId, token))
            return Result<PagedResponse<ZoneResponse>>.Fail(ErrorType.Conflict, "User doesn't belong to company");

        var zones = await context

    }
}