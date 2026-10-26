using Domain.Models;
using Infrastructure;
using Infrastructure.ResponsePatterns;

namespace Application.Features;
public interface ICompanyAcessValidator
{
    Task<Result<User>> EnsureUserIsAdminAsync(WmsDbContext context, Ulid userId, string ErrorMsg, CancellationToken token);
    Task<bool> EnsureUserExistsAsync(WmsDbContext context, Ulid userId, CancellationToken token);
    Task<Result<User>> EnsureUserHasAnyRoleAsync(WmsDbContext context, Ulid userId, string ErrorMsg, IReadOnlyCollection<UserType> types, CancellationToken token);
}