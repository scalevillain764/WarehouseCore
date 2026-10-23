using Application.Companies.Requests;
using Application.Companies.Responses;
using Infrastructure.ResponsePatterns;
using Microsoft.IdentityModel.Tokens;
namespace Application.Companies;

public interface ICompanyService
{
    Task<Result<CompanyResponse>> GetCurrentCompanyAsync(Ulid userId, CancellationToken token);
    Task<Result<CompanyResponse>> EditCurrentCompanyAsync(Ulid userId, EditCompanyRequest request, CancellationToken token);
};