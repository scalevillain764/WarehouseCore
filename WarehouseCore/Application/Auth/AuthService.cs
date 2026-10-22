using Application.Auth.Requests;
using Application.Auth.Responses;
using Application.Auth.TokenService;
using Application.CompanyContext;
using Application.Features;
using Domain.Models;
using Infrastructure;
using Infrastructure.ResponsePatterns;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
namespace Application.Auth;

public class AuthService : IAuthService
{
    private readonly RegistryDbContext _registryContext;
    private readonly IWmsDbContextFactory _dbFactory;
    public AuthService(IWmsDbContextFactory dbFactory, RegistryDbContext registryContext)
    {
        _dbFactory = dbFactory;
        _registryContext = registryContext;
    }
    private async Task<Result<RegistrateFirstAdminResponse>> RegistrateFirstAdminAsync(RegistrateFirstAdminRequest request, CancellationToken token, WmsDbContext _context, Ulid CompanyId)
    {
        if (await _context.Users.AnyAsync())
            return Result<RegistrateFirstAdminResponse>.Fail(ErrorType.Conflict, "You cannot create an admin");

        var hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password);

        var firstAdmin = new User(UserType.Admin, request.FirstName, request.SecondName,
            request.Email, hashedPassword, null, null, CompanyId);

        _context.Users.Add(firstAdmin);

        return Result<RegistrateFirstAdminResponse>.Success(new RegistrateFirstAdminResponse(firstAdmin.Email, request.Password));
    }

    public async Task<Result<RegistrateCompanyResponse>> RegistrateCompanyAsync(RegistrateCompanyRequest request, CancellationToken token)
    {
        var builder = new NpgsqlCommandBuilder();

        // Creating company to public table companies
        var newCompany = new Company(request.CompanyName, request.Code);

        _registryContext.Companies.Add(newCompany);

        await _registryContext
            .SaveChangesAsync(token);

        // Creating schema for copmany
        var quotedSchemaName = builder
            .QuoteIdentifier(newCompany.SchemaName);

        await _registryContext.Database
            .ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS {quotedSchemaName}", token);

        // Creating context for current company
        await using var _context = await _dbFactory
            .GetContextAsync(newCompany.Id, token);

        try
        {
            await _context.Database
            .MigrateAsync(token);
        }
        catch (Exception ex)
        {
            _registryContext.Remove(newCompany);
            await _registryContext.SaveChangesAsync(token);
            throw; // to exception middleware
        }

        // Adding admin to current company
        var firstAdminRequest = await RegistrateFirstAdminAsync(request.adminRequest, token, _context, newCompany.Id);

        if (!firstAdminRequest.IsSuccess)
        {
            _registryContext.Remove(newCompany);
            await _registryContext.SaveChangesAsync(token);

            return Result<RegistrateCompanyResponse>.Fail((ErrorType)firstAdminRequest.ErrorType!, firstAdminRequest.ErrorMessage!);
        }

        await _context.SaveChangesAsync(token);

        return Result<RegistrateCompanyResponse>.Success(new RegistrateCompanyResponse(newCompany.Code, firstAdminRequest.Data!));
    }
}