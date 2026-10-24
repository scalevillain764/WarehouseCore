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
    public async Task<Result<User>>_companyValidator.EnsureUserIsAdminAsync(WmsDbContext context, Ulid userId, string ErrorMsg, CancellationToken token)
    {
        var user = await context.Users
            .Where(x => x.CompanyId == _currentCompany.CompanyId)
            .FirstOrDefaultAsync(x => x.Id == userId, token);

        if (user is null)
            return Result<User>.Fail(ErrorType.NotFound, "User not found");

        if (user.Type != Domain.Models.UserType.Admin)
            return Result<User>.Fail(ErrorType.Forbidden, ErrorMsg);

        return Result<User>.Success(user);
    }

    public async Task<bool> EnsureUserExistsAsync(WmsDbContext context, Ulid userId, CancellationToken token)
        => await context.Users.AnyAsync(x => x.Id == userId, token);
}