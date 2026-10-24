using Application.Companies.Requests;
using Application.Companies.Responses;
using Application.CompanyContext;
using Application.Features;
using Application.Warehouses.Responses;
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

    public async Task<Result<CompanyResponse>> GetCurrentCompanyAsync(Ulid userId, CancellationToken token)
    {
        await using var context = await _wmsContextFactory
            .GetContextAsync(_companyContext.CompanyId, token);

        var company = await _registryDbContext.Companies         
            .FindAsync(_companyContext.CompanyId, token);

        if (company is null)
            return Result<CompanyResponse>.Fail(ErrorType.NotFound, "Company not found");

        bool userExistsAtCompany = await context.Users
           .AnyAsync(x => x.Id == userId, token);

        if (!userExistsAtCompany)
            return Result<CompanyResponse>.Fail(ErrorType.Forbidden, "User doesn't belong to this company");

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

        var user = await context.Users
            .Where(x => x.CompanyId == _companyContext.CompanyId)
            .FirstOrDefaultAsync(x => x.Id == userId, token);

        if (user is null)
            return Result<CompanyResponse>.Fail(ErrorType.NotFound, "User not found");

        if (user.Type != Domain.Models.UserType.Admin)
            return Result<CompanyResponse>.Fail(ErrorType.Forbidden, "Only admins can edit company properties");

        bool nameExists = await _registryDbContext.Companies
            .AnyAsync(x => x.Name == request.Name && x.Id != _companyContext.CompanyId, token);

        if (nameExists)
            return Result<CompanyResponse>.Fail(ErrorType.Conflict, "Company name already exists");

        company.Name = request.Name;

        await _registryDbContext.SaveChangesAsync(token);

        return Result<CompanyResponse>.Success(new CompanyResponse(company));
    }


}