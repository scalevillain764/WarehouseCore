namespace Application.Auth.TokenService;
public interface ITokenService
{
    string CreateRefreshToken();
    string CreateAccessToken(Ulid Id, string Name, Ulid CompanyId);
}