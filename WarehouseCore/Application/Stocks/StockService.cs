using Infrastructure.ResponsePatterns;
using Application.Stocks.Requests;
using Application.Stocks.Responses;
using Infrastructure;
using Application.Features;
using Application.CompanyContext;
using Microsoft.EntityFrameworkCore;
using System.Runtime.Intrinsics.X86;
using Domain.Models;
namespace Application.Stocks;
public class StockService : IStockService
{
    private readonly IWmsDbContextFactory _factory;
    private readonly ICurrentCompanyContext _currentCompanyContext;
    private readonly ICompanyAcessValidator _companyValidator;
    public StockService(IWmsDbContextFactory factory, ICurrentCompanyContext currentCompanyContext, ICompanyAcessValidator companyValidator)
    {
        _factory = factory;
        _currentCompanyContext = currentCompanyContext;
        _companyValidator = companyValidator;
    }
    public async Task<Result<StockResponse>> CreateStockAsync(Ulid userId, CreateStockRequest request, CancellationToken token)
    {
        await using var context = await _factory
            .GetContextAsync(_currentCompanyContext.CompanyId, token);

        if (!await _companyValidator.EnsureUserExistsAsync(context, userId, token))
            return Result<StockResponse>.Fail(ErrorType.Conflict, "User doesn't belong to company");

        var bin = await context.Bins
            .FirstOrDefaultAsync(x => x.Id == request.BinId, token);

        if (bin is null)
            return Result<StockResponse>.Fail(ErrorType.NotFound, "Bin not found");

        var product = await context.Products
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, token);

        if (product is null)
            return Result<StockResponse>.Fail(ErrorType.NotFound, "Product not found");

        var stock = new Stock(bin.Id, request.ProductId, request.Quantity);

        context.Stocks.Add(stock);

        return Result<StockResponse>.Success(new StockResponse)
    }
}