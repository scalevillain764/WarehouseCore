using Application.CompanyContext;
using Domain.Models;
using Infrastructure;
using Infrastructure.ResponsePatterns;
using Microsoft.EntityFrameworkCore;

namespace Application.Features;
public class CompanyAcessValidator : ICompanyAcessValidator
{
    private ICurrentCompanyContext _currentCompany;
    public CompanyAcessValidator(ICurrentCompanyContext companyContext)
    {
        _currentCompany = companyContext;
    }
    private async Task<Result<User>> EnsureUserIsAsync(WmsDbContext context, Ulid userId, string ErrorMsg, UserType type, CancellationToken token)
    {
        var user = await context.Users
           .Where(x => x.CompanyId == _currentCompany.CompanyId)
           .FirstOrDefaultAsync(x => x.Id == userId, token);

        if (user is null)
            return Result<User>.Fail(ErrorType.NotFound, "User not found");

        if (user.Type != type)
            return Result<User>.Fail(ErrorType.Forbidden, ErrorMsg);

        return Result<User>.Success(user);
    }
    public async Task<Result<User>> EnsureUserIsAdminAsync(WmsDbContext context, Ulid userId, string ErrorMsg, CancellationToken token)
        => await EnsureUserIsAsync(context, userId, ErrorMsg, UserType.Admin, token);

    public async Task<Result<User>> EnsureUserIsManagerAsync(WmsDbContext context, Ulid userId, string ErrorMsg, CancellationToken token)
        => await EnsureUserIsAsync(context, userId, ErrorMsg, UserType.Manager, token);

    public async Task<bool> EnsureUserExistsAsync(WmsDbContext context, Ulid userId, CancellationToken token)
        => await context.Users.AnyAsync(x => x.Id == userId, token);
}