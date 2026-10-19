using Infrastructure;

namespace Application.Features;
public interface IWmsDbContextFactory
{
    Task<WmsDbContext> GetContextAsync(Ulid CompanyId, CancellationToken token);
}