namespace Application.Auth.Requests;
public sealed record RegistrateCompanyRequest(string CompanyName, string Code, RegistrateFirstAdminRequest adminRequest);