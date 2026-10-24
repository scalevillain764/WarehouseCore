using Application.Companies.Requests;
using Application.Companies.Responses;
using Application.CompanyContext;
using Application.Features;
using Application.Warehouses.Responses;
using Domain.Models;
using Infrastructure;
using Infrastructure.ResponsePatterns;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
namespace Application.Companies;
public class CompanyService : ICompanyService
{
    private readonly IWmsDbContextFactory _wmsContextFactory;
    private readonly ICurrentCompanyContext _companyContext;
    private readonly RegistryDbContext _registryDbContext;
    public CompanyService(IWmsDbContextFactory wmsContextFactory, ICurrentCompanyContext companyContext, RegistryDbContext registryDbContext)
    {
        _wmsContextFactory = wmsContextFactory;
        _companyContext = companyContext;
        _registryDbContext = registryDbContext;
    }
    private async Task<Result<User>> EnsureUserIsAdminAsync(WmsDbContext context, Ulid userId, string ErrorMsg, CancellationToken token)
    {
        var user = await context.Users
            .Where(x => x.CompanyId == _companyContext.CompanyId)
            .FirstOrDefaultAsync(x => x.Id == userId, token);

        if (user is null)
            return Result<User>.Fail(ErrorType.NotFound, "User not found");

        if (user.Type != Domain.Models.UserType.Admin)
            return Result<User>.Fail(ErrorType.Forbidden, ErrorMsg);

        return Result<User>.Success(user);
    }

    private async Task<bool> EnsureUserExistsAsync(WmsDbContext context, Ulid userId, CancellationToken token)
        => await context.Users.AnyAsync(x => x.Id == userId, token);

    public async Task<Result<CompanyResponse>> GetCurrentCompanyAsync(Ulid userId, CancellationToken token)
    {
        await using var context = await _wmsContextFactory
            .GetContextAsync(_companyContext.CompanyId, token);

        var company = await _registryDbContext.Companies         
            .FindAsync(_companyContext.CompanyId, token);

        if (company is null)
            return Result<CompanyResponse>.Fail(ErrorType.NotFound, "Company not found");

        if (await EnsureUserExistsAsync(context, userId, token))
            return Result<CompanyResponse>.Fail(ErrorType.Conflict, "User doesn't belong to company");

        return Result<CompanyResponse>.Success(new CompanyResponse(company));
    }

    public async Task<Result<CompanyResponse>> EditCurrentCompanyAsync(Ulid userId, EditCompanyRequest request, CancellationToken token)
    {
        await using var context = await _wmsContextFactory
            .GetContextAsync(_companyContext.CompanyId, token);

        var company = await _registryDbContext.Companies
            .FindAsync(_companyContext.CompanyId, token);

        if (company is null)
            return Result<CompanyResponse>.Fail(ErrorType.NotFound, "Company not found");

        var userRequest = await EnsureUserIsAdminAsync(context, userId, "Only admin can edit company properties", token);
        if (!userRequest.IsSuccess)
            return Result<CompanyResponse>.Fail((ErrorType)userRequest.ErrorType, userRequest.ErrorMessage);

        bool nameExists = await _registryDbContext.Companies
            .AnyAsync(x => x.Name == request.Name && x.Id != _companyContext.CompanyId, token);

        if (nameExists)
            return Result<CompanyResponse>.Fail(ErrorType.Conflict, "Company name already exists");

        company.Name = request.Name;

        await _registryDbContext.SaveChangesAsync(token);

        return Result<CompanyResponse>.Success(new CompanyResponse(company));
    }


}