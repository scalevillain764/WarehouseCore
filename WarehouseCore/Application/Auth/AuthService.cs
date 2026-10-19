using Application.Features;
using Application.Auth.TokenService;
using Application.Auth.Responses;
using Application.CompanyContext;
using Application.Auth.Requests;
using Infrastructure.ResponsePatterns;
using Domain.Models;
namespace Application.Auth;

public class AuthService : IAuthService
{
    private readonly IWmsDbContextFactory _dbFactory;
    private readonly ICurrentCompanyContext _companyContext;
    public AuthService(IWmsDbContextFactory dbFactory, ICurrentCompanyContext companyContext)
    {
        _dbFactory = dbFactory;
        _companyContext = companyContext;
    }
    public async Task<Result<RegistrateFirstAdminResponse>> RegistrateFirstAdminAsync(RegistrateFirstAdminRequest request, CancellationToken token)
    {
        await using var _context = await _dbFactory.GetContextAsync(_companyContext.CompanyId, token);

        if (_context is null)
            return Result<RegistrateFirstAdminResponse>.Fail(ErrorType.Conflict, "Current company context is not found");

        if (_context.Users.Any())
            return Result<RegistrateFirstAdminResponse>.Fail(ErrorType.Conflict, "You cannot create an admin");

        var hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password);

        var firstAdmin = new User(UserType.Admin, request.FirstName, request.SecondName,
            request.Email, hashedPassword, null, null, _companyContext.CompanyId);

        _context.Users.Add(firstAdmin);

        return Result<RegistrateFirstAdminResponse>.Success(new RegistrateFirstAdminResponse(firstAdmin.Email, request.Password));
    }

    public async Task<Result<RegistrateFirstAdminRequest>> RegistrateCompanyAsync(RegistrateCompanyRequest request, CancellationToken token)
    {

    }
}