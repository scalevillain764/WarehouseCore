
using Application.Auth.Requests;
using Application.Auth.Responses;
using Application.Auth.TokenService;
using Application.CompanyContext;
using Application.Features;
using BCrypt.Net;
using Domain.Models;
using Infrastructure;
using Infrastructure.ResponsePatterns;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Application.Auth;

public class AuthService : IAuthService
{
    private readonly RegistryDbContext _registryContext;
    private readonly UserLoginCreator _loginCreator;
    private readonly IWmsDbContextFactory _dbFactory;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentCompanyContext _currentCompanyContext;

    public AuthService(
        IWmsDbContextFactory dbFactory,
        ITokenService tokenService,
        RegistryDbContext registryContext,
        UserLoginCreator loginCreator,
        IHttpContextAccessor httpContextAccessor,
        ICurrentCompanyContext currentCompanyContext)
    {
        _dbFactory = dbFactory;
        _registryContext = registryContext;
        _loginCreator = loginCreator;
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
        _currentCompanyContext = currentCompanyContext;
    }

    // Creating first admin for a newly registered company
    private async Task<Result<RegistrateFirstAdminResponse>> RegistrateFirstAdminAsync(RegistrateFirstAdminRequest request, CancellationToken token,
            WmsDbContext context, Ulid companyId)
    {
        if (await context.Users.AnyAsync(token))
        {
            return Result<RegistrateFirstAdminResponse>.Fail(
                ErrorType.Conflict,
                "You cannot create an admin");
        }

        var passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(
            request.Password,
            workFactor: 11);

        var userId = Ulid.NewUlid();

        var login = _loginCreator.CreateLogin(
            userId,
            request.FirstName,
            request.SecondName);

        var firstAdmin = new User(
            userId,
            request.FirstName,
            request.SecondName,
            login,
            passwordHash,
            companyId);

        context.Users.Add(firstAdmin);

        await context.SaveChangesAsync(token);

        return Result<RegistrateFirstAdminResponse>.Success(new RegistrateFirstAdminResponse(firstAdmin.Login, request.Password));
    }

    // Registering a new company
    public async Task<Result<RegistrateCompanyResponse>> RegistrateCompanyAsync(RegistrateCompanyRequest request, CancellationToken token)
    {
        var builder = new NpgsqlCommandBuilder();

        var newCompany = new Company(
            request.CompanyName,
            request.Code);

        _registryContext.Companies.Add(newCompany);

        await _registryContext.SaveChangesAsync(token);

        var quotedSchemaName = builder.QuoteIdentifier(
            newCompany.SchemaName);

        await _registryContext.Database.ExecuteSqlRawAsync(
            $"CREATE SCHEMA IF NOT EXISTS {quotedSchemaName}",
            token);

        await using var context = await _dbFactory.GetContextAsync(
            newCompany.Id,
            token);

        try
        {
            await context.Database.MigrateAsync(token);

            var adminResult = await RegistrateFirstAdminAsync(
                request.adminRequest,
                token,
                context,
                newCompany.Id);

            if (!adminResult.IsSuccess)
            {
                await _registryContext.Database.ExecuteSqlRawAsync(
                    $"DROP SCHEMA IF EXISTS {quotedSchemaName} CASCADE",
                    token);

                _registryContext.Companies.Remove(newCompany);
                await _registryContext.SaveChangesAsync(token);

                return Result<RegistrateCompanyResponse>.Fail(
                    (ErrorType)adminResult.ErrorType!,
                    adminResult.ErrorMessage!);
            }

            return Result<RegistrateCompanyResponse>.Success(
                new RegistrateCompanyResponse(
                    newCompany.Code,
                    adminResult.Data!));
        }
        catch
        {
            await _registryContext.Database.ExecuteSqlRawAsync(
                $"DROP SCHEMA IF EXISTS {quotedSchemaName} CASCADE",
                token);

            _registryContext.Companies.Remove(newCompany);
            await _registryContext.SaveChangesAsync(token);

            throw;
        }
    }

    // Registering an employee inside the current company
    public async Task<Result<RegistrateUserResponse>> RegistrateUserAsync(RegistrateUserRequest request, CancellationToken token, Ulid companyId)
    {
        await using var context = await _dbFactory.GetContextAsync(
            companyId,
            token);

        var userId = Ulid.NewUlid();

        var login = _loginCreator.CreateLogin(
            userId,
            request.FirstName,
            request.SecondName);

        if (await context.Users.AnyAsync(
            x => x.Login == login,
            token))
        {
            return Result<RegistrateUserResponse>.Fail(
                ErrorType.Conflict,
                "Login already exists");
        }

        var passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(
            request.Password,
            workFactor: 11);

        var user = new User(
            userId,
            request.Type,
            request.FirstName,
            request.SecondName,
            login,
            passwordHash,
            companyId);

        context.Users.Add(user);

        await context.SaveChangesAsync(token);

        return Result<RegistrateUserResponse>.Success(
            new RegistrateUserResponse(user.Id, user.Login));
    }

    // User login
    public async Task<Result<LogInUserResponse>> LogInUserAsync(LogInUserRequest request, CancellationToken token)
    {
        var company = await _registryContext.Companies
            .FirstOrDefaultAsync(
                x => x.Code == request.CompanyCode,
                token);

        if (company is null || !company.IsActive)
        {
            return Result<LogInUserResponse>.Fail(
                ErrorType.NotFound,
                "Company not found");
        }

        await using var context = await _dbFactory.GetContextAsync(
            company.Id,
            token);

        var user = await context.Users
            .FirstOrDefaultAsync(
                x => x.Login == request.Login,
                token);

        if (user is null ||
            !BCrypt.Net.BCrypt.EnhancedVerify(
                request.Password,
                user.PasswordHash))
        {
            return Result<LogInUserResponse>.Fail(
                ErrorType.Unauthorized,
                "Invalid credentials");
        }

        var accessToken = _tokenService.AppendCookiesAndGetAccessToken(
            user,
            company.Id);

        await context.SaveChangesAsync(token);

        return Result<LogInUserResponse>.Success(
            new LogInUserResponse(
                user.Id,
                user.Login,
                accessToken));
    }

    // Refreshing access and refresh tokens
    public async Task<Result<LogInUserResponse>> RefreshAsync(Ulid userId, string companyCode, CancellationToken token)
    {
        var existingRefreshToken =
            _httpContextAccessor.HttpContext?
                .Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(existingRefreshToken))
        {
            return Result<LogInUserResponse>.Fail(
                ErrorType.Unauthorized,
                "Refresh token is missing");
        }

        var company = await _registryContext.Companies
            .FirstOrDefaultAsync(
                x => x.Code == companyCode,
                token);

        if (company is null || !company.IsActive)
        {
            return Result<LogInUserResponse>.Fail(
                ErrorType.Unauthorized,
                "Invalid session");
        }

        await using var context = await _dbFactory.GetContextAsync(
            company.Id,
            token);

        var user = await context.Users
            .FirstOrDefaultAsync(
                x => x.Id == userId,
                token);

        if (user is null)
        {
            return Result<LogInUserResponse>.Fail(
                ErrorType.Unauthorized,
                "Invalid session");
        }

        if (user.RefreshTokenExpiersAt is null ||
            user.RefreshTokenExpiersAt <= DateTime.UtcNow)
        {
            return Result<LogInUserResponse>.Fail(
                ErrorType.Unauthorized,
                "Session expired");
        }

        if (string.IsNullOrEmpty(user.RefreshTokenHash) ||
            !BCrypt.Net.BCrypt.Verify(
                existingRefreshToken,
                user.RefreshTokenHash))
        {
            return Result<LogInUserResponse>.Fail(
                ErrorType.Unauthorized,
                "Invalid session");
        }

        var accessToken = _tokenService.AppendCookiesAndGetAccessToken(
            user,
            company.Id);

        await context.SaveChangesAsync(token);

        return Result<LogInUserResponse>.Success(
            new LogInUserResponse(
                user.Id,
                user.Login,
                accessToken));
    }
}
