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
    private async Task<Result<User>> EnsureUserIsAsync(WmsDbContext context, Ulid userId, string ErrorMsg, IReadOnlyCollection<UserType> types, CancellationToken token)
    {
        var user = await context.Users
           .Where(x => x.CompanyId == _currentCompany.CompanyId)
           .FirstOrDefaultAsync(x => x.Id == userId, token);

        if (user is null)
            return Result<User>.Fail(ErrorType.NotFound, "User not found");

        return !types.Contains(user.Type) ? Result<User>.Fail(ErrorType.Forbidden, ErrorMsg) : Result<User>.Success(user);
    }
    public async Task<Result<User>> EnsureUserIsAdminAsync(WmsDbContext context, Ulid userId, string ErrorMsg, CancellationToken token)
        => await EnsureUserIsAsync(context, userId, ErrorMsg, new List<UserType> { UserType.Admin }, token);

    public async Task<Result<User>> EnsureUserHasAnyRoleAsync(WmsDbContext context, Ulid userId, string ErrorMsg, IReadOnlyCollection<UserType> types, CancellationToken token)
        => await EnsureUserIsAsync(context, userId, ErrorMsg, types, token);

    public async Task<bool> EnsureUserExistsAsync(WmsDbContext context, Ulid userId, CancellationToken token)
        => await context.Users.AnyAsync(x => x.Id == userId, token);
}