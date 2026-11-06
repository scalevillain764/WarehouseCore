using Application.CompanyContext;
using Application.Features;
using Application.WarehouseOperations.Requests;
using Application.WarehouseOperations.Responses;
using Domain.Models;
using Application.Stocks;
using Infrastructure.ResponsePatterns;
using Application.Stocks.Requests;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Application.WarehouseOperations;
public class WarehouseOperationService : IWarehouseOperationService
{
    private readonly IWmsDbContextFactory _wmsFactory;
    private readonly ICurrentCompanyContext _currentCompany;
    private readonly ICompanyAcessValidator _companyValidator;
    private readonly IStockService _stockService;

    public WarehouseOperationService(IWmsDbContextFactory wmsFactory, ICurrentCompanyContext currentCompany,
        ICompanyAcessValidator companyValidator, IStockService stockService)
    {
        _wmsFactory = wmsFactory;
        _currentCompany = currentCompany;
        _companyValidator = companyValidator;
        _stockService = stockService;
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
        if (request.Page < 1 || request.PageSize < 1)
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
                x.OperationNumber == searchTerm || x.OperationNumber.Contains(searchTerm));
        }

        if(request.Types is not null && request.Types.Count > 0)
            warehouseOperationsQueryable = warehouseOperationsQueryable.Where(x => request.Types
                   .Contains(x.Type));

        if (request.Statuses is not null && request.Statuses.Count > 0)
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
                .Where(x => x.CreatedBy!.FirstName == request.FirstName);

        if(request.SecondName is not null)
            warehouseOperationsQueryable = warehouseOperationsQueryable
                .Where(x => x.CreatedBy!.SecondName == request.SecondName);

        if (request.WorkerType is not null)
            warehouseOperationsQueryable = warehouseOperationsQueryable
                .Where(x => x.CreatedBy!.Type == request.WorkerType);

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

        if (!await _companyValidator.EnsureUserExistsAsync(context, userId, token))
            return Result<WarehouseOperationDetailsResponse>.Fail(ErrorType.NotFound, "User doesn't belong to company");

        var warehouseOperation = await context.WarehouseOperations
            .Include(x => x.Items)
            .Where(x => x.AssignedToId == userId)
            .FirstOrDefaultAsync(x => x.Id == warehouseOperationId, token);

        if (warehouseOperation is null)
            return Result<WarehouseOperationDetailsResponse>.Fail(ErrorType.NotFound, "Warehouse operation not found");

        if(warehouseOperation.Status is not WarehouseOperationStatus.InProgress)
        {
            string postfix = warehouseOperation.Status switch
            {
                WarehouseOperationStatus.Completed => "already completed",
                WarehouseOperationStatus.Cancelled => "already cancelled",
                _ => "not in progress"
            };
            return Result<WarehouseOperationDetailsResponse>.Fail(ErrorType.Conflict, $"Warehouse operation already {postfix}");
        }

        var type = warehouseOperation.Type;
        var items = warehouseOperation.Items;

        using var transaction = await context.Database.BeginTransactionAsync(token);

        foreach(var item in items)
        {
            switch(warehouseOperation.Type)
            {
                case WarehouseOperationType.Transfer:
                    {
                        var transferOperation = await _stockService
                            .TransferStockAsync(context, 
                                new TransferStockRequest((Ulid)item.SourceBinId, 
                                    (Ulid)item.DestinationBinId, item.ProductId, item.PlannedQuantity), 
                                token);

                        if (!transferOperation.IsSuccess)
                            return Result<WarehouseOperationDetailsResponse>
                                .Fail((ErrorType)transferOperation.ErrorType!, transferOperation.ErrorMessage!);

                        break;
                    }
                case WarehouseOperationType.Receiving:
                    {
                        var addStockOperation = await _stockService
                            .AddStockAsync(context, 
                            new CreateStockRequest((Ulid)item.DestinationBinId!, item.ProductId, item.PlannedQuantity), 
                            token);

                        if (!addStockOperation.IsSuccess)
                            return Result<WarehouseOperationDetailsResponse>
                                .Fail((ErrorType)addStockOperation.ErrorType, addStockOperation.ErrorMessage!);

                        break;
                    }
                case WarehouseOperationType.Putaway:
                    {
                        var putawayOperation = await _stockService
                            .TransferStockAsync(context, new TransferStockRequest((Ulid)item.SourceBinId,
                                    (Ulid)item.DestinationBinId, item.ProductId, item.PlannedQuantity),
                                token);

                        if(!putawayOperation.IsSuccess)
                            return Result<WarehouseOperationDetailsResponse>
                                .Fail((ErrorType)putawayOperation.ErrorType!, putawayOperation.ErrorMessage!);

                        break;
                    }
                case WarehouseOperationType.Picking:
                    {
                        var pickingOperation = await _stockService
                            .TransferStockAsync(context, new TransferStockRequest((Ulid)item.SourceBinId,
                                    (Ulid)item.DestinationBinId, item.ProductId, item.PlannedQuantity),
                                token);

                        if (!pickingOperation.IsSuccess)
                            return Result<WarehouseOperationDetailsResponse>
                                .Fail((ErrorType)pickingOperation.ErrorType!, pickingOperation.ErrorMessage!);

                        break;
                    }
                case WarehouseOperationType.Shipping:
                    {
                        var removeStockOperation = await _stockService
                            .RemoveStockAsync(context, 
                                new EditStockRequest((Ulid)item.SourceBinId, item.ProductId, item.PlannedQuantity), 
                                token);

                        if (!removeStockOperation.IsSuccess)
                            return Result<WarehouseOperationDetailsResponse>
                                .Fail((ErrorType)removeStockOperation.ErrorType, removeStockOperation.ErrorMessage!);

                        break;
                    }
            }
        }

        warehouseOperation.CompletedAt = DateTime.UtcNow;
        warehouseOperation.Status = WarehouseOperationStatus.Completed;

        await context.SaveChangesAsync(token);
        await context.Database.CommitTransactionAsync(token);

        return Result<WarehouseOperationDetailsResponse>.Success(new WarehouseOperationDetailsResponse(warehouseOperation));
    }

    public async Task<Result<WarehouseOperationDetailsResponse>> CancelWarehouseOperationAsync(Ulid userId,
        Ulid warehouseOperationId, CancellationToken token)
    {
        await using var context = await _wmsFactory
          .GetContextAsync(_currentCompany.CompanyId, token);

        if (!await _companyValidator.EnsureUserExistsAsync(context, userId, token))
            return Result<WarehouseOperationDetailsResponse>.Fail(ErrorType.NotFound, "User doesn't belong to company");

        var warehouseOperation = await context.WarehouseOperations
            .Where(x => x.AssignedToId == userId)
            .FirstOrDefaultAsync(x => x.Id == warehouseOperationId, token);

        if (warehouseOperation is null)
            return Result<WarehouseOperationDetailsResponse>.Fail(ErrorType.NotFound, "Warehouse operation not found");

        if (warehouseOperation.Status is not WarehouseOperationStatus.InProgress)
        {
            string postfix = warehouseOperation.Status switch
            {
                WarehouseOperationStatus.Completed => "already completed",
                WarehouseOperationStatus.Cancelled => "already cancelled",
                _ => "not in progress"
            };
            return Result<WarehouseOperationDetailsResponse>.Fail(ErrorType.Conflict, $"Warehouse operation already {postfix}");
        }

        warehouseOperation.Status = WarehouseOperationStatus.Cancelled;

        await context.SaveChangesAsync(token);

        return Result<WarehouseOperationDetailsResponse>.Success(new WarehouseOperationDetailsResponse(warehouseOperation));
    }
}