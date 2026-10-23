using Domain.Models;
namespace Application.Companies.Responses;
public sealed record CompanyResponse(
    Ulid Id,
    string Name)
{
    public CompanyResponse (Company c) :
        this(c.Id, c.Name)
    { }
}
  