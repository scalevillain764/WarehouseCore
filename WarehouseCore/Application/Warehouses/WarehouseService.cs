using Application.CompanyContext;
using Application.Features;
using Application.Warehouses.Requests;
using Application.Warehouses.Responses;
using Infrastructure;
using Infrastructure.ResponsePatterns;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Warehouses;
public class WarehouseService : IWarehouseService
{
    private readonly IWmsDbContextFactory _wmsContextFactory;
    private readonly ICurrentCompanyContext _currentCompany;
  
    public WarehouseService(IWmsDbContextFactory wmsContextFactory, ICurrentCompanyContext currentCompany)
    {
        _wmsContextFactory = wmsContextFactory;
        _currentCompany = currentCompany;
    }

    private void SetWarehouseProperties(Warehouse warehouse, EditWarehouseRequest request)
    {
        warehouse.Name = request.WarehouseName;
        warehouse.Address.Country = request.Country;
        warehouse.Address.City = request.City;
        warehouse.Address.Street = request.Street;
        warehouse.Address.NumberBuilding = request.NumberBuilding;
    }
    
    public async Task<Result<WarehouseResponse>> CreateWarehouseAsync(Ulid userId, CreateWarehouseRequest request, CancellationToken token)
    {
        await using var context = await _wmsContextFactory
            .GetContextAsync(_currentCompany.CompanyId, token);

        bool userExistsAtCompany = await context.Users
            .AnyAsync(x => x.Id == userId, token);

        if (!userExistsAtCompany)
            return Result<WarehouseResponse>.Fail(ErrorType.Forbidden, "User doesn't belong to this company");

        bool warehouseExists = await context.Warehouses
            .AnyAsync(x => x.Name == request.WarehouseName, token);

        if (warehouseExists)
            return Result<WarehouseResponse>.Fail(ErrorType.Conflict, "Warehouse with this name already exists");

        var warehouse = new Warehouse(request.WarehouseName, 
            new Address(request.Country, request.City, request.Street, request.NumberBuilding), _currentCompany.CompanyId);

        context.Warehouses.Add(warehouse);

        await context.SaveChangesAsync(token);

        return Result<WarehouseResponse>.Success(new WarehouseResponse(warehouse));
    }

    public async Task<Result<string>> RemoveWarehouseAsync(Ulid userId, Ulid warehouseId, CancellationToken token)
    {
        await using var context = await _wmsContextFactory
           .GetContextAsync(_currentCompany.CompanyId, token);

        bool userExistsAtCompany = await context.Users
            .AnyAsync(x => x.Id == userId, token);

        if (!userExistsAtCompany)
            return Result<string>.Fail(ErrorType.Forbidden, "User doesn't belong to this company");

        var warehouse = await context.Warehouses
            .Where(x => x.IsActive)
             .Include(x => x.Zones)
                .ThenInclude(z => z.Bins)
            .FirstOrDefaultAsync(x => x.Id == warehouseId, token);

        if (warehouse is null)
            return Result<string>.Fail(ErrorType.NotFound, "Warehouse not found");

        if (warehouse.Zones.Any(x => x.IsActive) || warehouse.Zones.Any(x => x.Bins.Any(b => b.IsActive)))
            return Result<string>.Fail(ErrorType.Conflict, "Warehouse contaitns active zones or active bins. Please, try again later");

        warehouse.IsActive = false;

        await context.SaveChangesAsync(token);

        return Result<string>.Success("OK");
    }

    public async Task<Result<WarehouseResponse>> EditWarehouseAsync(Ulid userId, EditWarehouseRequest request, CancellationToken token)
    {
        await using var context = await _wmsContextFactory
          .GetContextAsync(_currentCompany.CompanyId, token);

        bool userExistsAtCompany = await context.Users
            .AnyAsync(x => x.Id == userId, token);

        if (!userExistsAtCompany)
            return Result<WarehouseResponse>.Fail(ErrorType.Forbidden, "User doesn't belong to this company");

        var warehouse = await context.Warehouses
            .Where(x => x.IsActive)
            .Include(x => x.Address)
            .FirstOrDefaultAsync(x => x.Id == request.warehouseId, token);

        if (warehouse is null)
            return Result<WarehouseResponse>.Fail(ErrorType.NotFound, "Warehouse not found");

        bool nameExists = await context.Warehouses
            .AnyAsync(x => x.Name == request.WarehouseName && x.Id != request.warehouseId, token);

        if (nameExists)
            return Result<WarehouseResponse>.Fail(ErrorType.Conflict, "This name already exists. Try another name");

        SetWarehouseProperties(warehouse, request);

        await context.SaveChangesAsync(token);

        return Result<WarehouseResponse>.Success(new WarehouseResponse(warehouse));
    }

    public async Task<Result<WarehouseResponse>> GetWarehouseAsync(Ulid userId, Ulid warehouseId, CancellationToken token)
    {
        await using var context = await _wmsContextFactory
         .GetContextAsync(_currentCompany.CompanyId, token);

        bool userExistsAtCompany = await context.Users
            .AnyAsync(x => x.Id == userId, token);

        if (!userExistsAtCompany)
            return Result<WarehouseResponse>.Fail(ErrorType.Forbidden, "User doesn't belong to this company");

        var warehouse = await context.Warehouses
            .Include(x => x.Address)
            .FirstOrDefaultAsync(x => x.Id == warehouseId, token);

        return warehouse is null ?
            Result<WarehouseResponse>.Fail(ErrorType.NotFound, "Warehouse not found")
            : Result<WarehouseResponse>.Success(new WarehouseResponse(warehouse));
    }
}