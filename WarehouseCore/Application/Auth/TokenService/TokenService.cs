using Domain.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Application.Auth.TokenService;
public class TokenService : ITokenService
{
    private readonly IHttpContextAccessor _accessor;
    public TokenService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }
    public string CreateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public string CreateAccessToken(Ulid Id, string Name, Ulid CompanyId)
    {
        Claim[] claims =
        {
             new Claim(ClaimTypes.NameIdentifier, Id.ToString()),
             new Claim(ClaimTypes.Name, Name),
             new Claim("CompanyId", CompanyId.ToString())
        };

        string? decoded_key = Environment.GetEnvironmentVariable("SECRET_KEY");

        if (string.IsNullOrEmpty(decoded_key))
            throw new KeyNotFoundException("Secret key not found");

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(decoded_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var token = new JwtSecurityToken(
               issuer: "WarehouseManagmnetSystemServer",
               audience: "WarehouseManagmnetSystemAudience",
               claims: claims,
               expires: DateTime.UtcNow.AddMinutes(10),
               signingCredentials: creds
               );

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    private string AppendCookiesAndGetAccessToken(User user, Ulid CompanyId)
    {
        string accessToken = CreateAccessToken(user.Id, user.FirstName, CompanyId);
        string refreshToken = CreateRefreshToken();

        string refreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);

        user.RefreshTokenHash = refreshTokenHash;
        user.RefreshTokenExpiersAt = DateTime.UtcNow.AddDays(7);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(7),
            SameSite = SameSiteMode.Strict,
            Secure = true
        };

        _accessor.HttpContext?.Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);

        return accessToken;
    }
}