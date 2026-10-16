using Application.CompanyContext;

namespace Application.Bins
{
    public class CurrentCompanyContext : ICurrentCompanyContext
    {
        private readonly HttpContextAccessor _accessor;
        public CurrentCompanyContext(HttpContextAccessor accessor)
        {
            _accessor = accessor;
        }
        public Ulid CompanyId =>
            Ulid.Parse(
            _accessor.HttpContext!
                .User.FindFirst("company_id")!.Value);
    }
}