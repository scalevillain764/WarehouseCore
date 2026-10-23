using Domain.Models;
namespace Application.Companies.Responses;
public sealed class CompanyRequest(
    Ulid Id,
    string Name)
{
    public CompanyRequest(Company c) :
        this(c.Id, c.Name)
    { }
}
  