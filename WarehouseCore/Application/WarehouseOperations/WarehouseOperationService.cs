using Application.CompanyContext;
using Application.Features;
using Application.WarehouseOperations.Requests;
using Application.WarehouseOperations.Responses;
using Domain.Models;
using Infrastructure.ResponsePatterns;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Result<WarehouseOperationDetailsResponse>> CreateWarehouseOperationAsync(Ulid userId, Ulid assignedToId,
        CreateWarehouseOperationRequest request, CancellationToken token)
    {
        await using var context = await _wmsFactory
            .GetContextAsync(_currentCompany.CompanyId, token);

        var adminAndManagerCheck = await _companyValidator.EnsureUserHasAnyRoleAsync(context, userId, "Only admins and managers can create warehouse operation",
            new List<UserType> { UserType.Admin, UserType.Manager }, token);

        if (!adminAndManagerCheck.IsSuccess)
            return Result<WarehouseOperationDetailsResponse>.Fail((ErrorType)adminAndManagerCheck.ErrorType, adminAndManagerCheck.ErrorMessage);

        if (!await _companyValidator.EnsureUserExistsAsync(context, assignedToId, token))
            return Result<WarehouseOperationDetailsResponse>.Fail(ErrorType.Conflict, "Assigned user doesn't belong to company");

        var warehouseOperation = new WarehouseOperation(request.Type, userId, assignedToId);

        context.WarehouseOperations.Add(warehouseOperation);

        await context.SaveChangesAsync(token);

        return Result<WarehouseOperationDetailsResponse>.Success(new WarehouseOperationDetailsResponse(warehouseOperation));
    }

    public async Task<Result<WarehouseOperationDetailsResponse>> GetWarehouseOperationByIdAsync(Ulid userId,
        Ulid warehouseOperationId, CancellationToken token)
    {
        await using var context = await _wmsFactory
            .GetContextAsync(_currentCompany.CompanyId, token);

        if (!await _companyValidator.EnsureUserExistsAsync(context, userId, token))
            return Result<WarehouseOperationDetailsResponse>.Fail(ErrorType.NotFound, "User doesn't belong to company");

        var warehouseOperation = await context.WarehouseOperations
            .Select(x => new WarehouseOperationDetailsResponse(
                x.Id,
                x.OperationNumber,
                x.Type.ToString(),
                x.Status.ToString(),
                x.CreatedAt,
                x.CompletedAt,
                x.CreatedById,
                x.CreatedBy!.FirstName,
                x.CreatedBy.SecondName,
                x.CreatedBy.Type.ToString()
            ))
           .FirstOrDefaultAsync(x => x.Id == warehouseOperationId, token);

        return warehouseOperation is not null ? Result<WarehouseOperationDetailsResponse>.Success(warehouseOperation) :
            Result<WarehouseOperationDetailsResponse>.Fail(ErrorType.NotFound, "Warehouse operation not found");
    }

    public async Task<Result<PagedResponse<WarehouseOperationResponse>>> GetWarehouseOperationsAsync(Ulid userId,
        GetWarehouseOperationsRequest request, CancellationToken token)
    {
        if (request.Page < 0 || request.PageSize < 0)
            return Result<PagedResponse<WarehouseOperationResponse>>.Fail(ErrorType.Validation, "Validation error");

        await using var context = await _wmsFactory
           .GetContextAsync(_currentCompany.CompanyId, token);

        if (!await _companyValidator.EnsureUserExistsAsync(context, userId, token))
            return Result<PagedResponse<WarehouseOperationResponse>>.Fail(ErrorType.NotFound, "User doesn't belong to company");

        var warehouseOperationsQueryable = context.WarehouseOperations
            .AsNoTracking()
            .Include(x => x.CreatedBy)
            .AsQueryable();

        if(request.OperationNumber is not null)
        {
            var searchTerm = request.OperationNumber.Trim();

            warehouseOperationsQueryable = warehouseOperationsQueryable.Where(x =>
                x.OperationNumber == searchTerm ||
                x.OperationNumber.Contains(searchTerm));
        }

        if(request.Types is not null)
            warehouseOperationsQueryable = warehouseOperationsQueryable.Where(x => request.Types
                   .Contains(x.Type));

        if (request.Statuses is not null)
            warehouseOperationsQueryable = warehouseOperationsQueryable.Where(x => request.Statuses
                    .Contains(x.Status));

        if (request.CompletedAt is not null)
            warehouseOperationsQueryable = warehouseOperationsQueryable
                .Where(x => x.CompletedAt == request.CompletedAt);

        if (request.CreatedById is not null)
            warehouseOperationsQueryable = warehouseOperationsQueryable
                .Where(x => x.CreatedById == request.CreatedById);

        if (request.FirstName is not null)
            warehouseOperationsQueryable = warehouseOperationsQueryable
                .Where(x => x.CreatedBy.FirstName == request.FirstName);

        if(request.SecondName is not null)
            warehouseOperationsQueryable = warehouseOperationsQueryable
                .Where(x => x.CreatedBy.SecondName == request.SecondName);

        if (request.WorkerType is not null)
            warehouseOperationsQueryable = warehouseOperationsQueryable
                .Where(x => x.CreatedBy.Type == request.WorkerType);

        if (request.CreatedAt is not null)
            warehouseOperationsQueryable = warehouseOperationsQueryable
                .Where(x => x.CreatedAt == request.CreatedAt);

        int totalCount = await warehouseOperationsQueryable
            .CountAsync(token);

        var rez = await warehouseOperationsQueryable
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new WarehouseOperationResponse(
                  x.Id,
                  x.OperationNumber,
                  x.Type.ToString(),
                  x.Status.ToString()
            ))
            .ToListAsync(token);

        return Result<PagedResponse<WarehouseOperationResponse>>
            .Success(new PagedResponse<WarehouseOperationResponse>(rez, request.Page, request.PageSize, totalCount));
    }

    public async Task<Result<WarehouseOperationDetailsResponse>> CompleteWarehouseOperationAsync(Ulid userId, Ulid warehouseOperationId, CancellationToken token)
    {
        await using var context = await _wmsFactory
            .GetContextAsync(_currentCompany.CompanyId, token);


    }

    Task<Result<WarehouseOperationDetailsResponse>> CancelWarehouseOperationAsync(Ulid userId,
        Ulid warehouseOperationId, CancellationToken token);
}