using System.Text.Json.Serialization;
namespace Application.Auth.Responses;
public sealed record RegistrateCompanyResponse(string Code, [property: JsonPropertyName("user")] RegistrateFirstAdminResponse adminResponse);