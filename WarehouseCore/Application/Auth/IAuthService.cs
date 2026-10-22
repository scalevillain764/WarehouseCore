using Application.Auth.Requests;
using Application.Auth.Responses;
using Infrastructure.ResponsePatterns;
namespace Application.Auth;
public interface IAuthService
{
    Task<Result<RegistrateCompanyResponse>> RegistrateCompanyAsync(RegistrateCompanyRequest request, CancellationToken token);
}