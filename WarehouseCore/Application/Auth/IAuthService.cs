using Application.Auth.Requests;
using Application.Auth.Responses;
using Infrastructure.ResponsePatterns;
namespace Application.Auth;
public interface IAuthService
{
    Task<Result<RegistrateFirstAdminRequest>> RegistrateCompanyAsync(RegistrateCompanyRequest request, CancellationToken token);
    Task<Result<RegistrateFirstAdminResponse>> RegistrateFirstAdminAsync(RegistrateFirstAdminRequest request, CancellationToken token);
}