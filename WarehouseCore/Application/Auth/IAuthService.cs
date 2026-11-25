using Application.Auth.Requests;
using Application.Auth.Responses;
using Infrastructure.ResponsePatterns;
namespace Application.Auth;
public interface IAuthService
{
    Task<Result<RegistrateCompanyResponse>> RegistrateCompanyAsync(RegistrateCompanyRequest request, CancellationToken token);
    Task<Result<RegistrateUserResponse>> RegistrateUserAsync(RegistrateUserRequest request, CancellationToken token, Ulid companyId);
    Task<Result<LogInUserResponse>> LogInUserAsync(LogInUserRequest request, CancellationToken token);
    Task<Result<LogInUserResponse>> RefreshAsync(Ulid userId, string companyCode, CancellationToken token);
}