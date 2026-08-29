
using System.Security.Claims;
using System.Text;
using ConnectGrowAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace ConnectGrowAPI.Services;
 
 //https://www.c-sharpcorner.com/article/jwt-auth-in-asp-net-mvc-secure-rest-api-with-c-sharp-net/

//https://www.youtube.com/watch?v=G6eEPPBIkh8 

//https://indotalent.com/blog/aspnet-core-identity-jwt-mvc.html?srsltid=AfmBOoqiAb5vq45DK35ztBkt-cFAbK2ZHskT8hoLLuuVfpe7vcYlOoPi

//https://www.descope.com/learn/post/refresh-token
//https://learn.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens?view=net-11.0-pp 
//https://towardsdev.com/understanding-refresh-tokens-in-web-api-development-with-c-17761a591cfc 
public class JwtOptions
{
    public const string SectionName = "Jwt";
 
    public string Key { get; set; } = string.Empty;
 
    public string Issuer { get; set; } = "csg-api";
    public string Audience { get; set; } = "csg-client";
 
    public int AccessTokenMinutes { get; set; } = 60;
 
    public int RefreshTokenDays { get; set; } = 7;
 
    public string? CookieDomain { get; set; }
}
 
public interface ITokenService
{
    //Signs a JWT containing the user's id, email and roles
    (string Token, DateTime ExpiresAt) CreateAccessToken(ApplicationUser user, IList<string> roles);
 
    (string RawToken, string TokenHash) CreateRefreshToken();
 
    //Hashes a presented refresh token so it can be matched against stored values
    string HashToken(string rawToken);
}
 
public class TokenService : ITokenService
{
    private readonly JwtOptions _options;
 
    public TokenService(Microsoft.Extensions.Options.IOptions<JwtOptions> options)
    {
        _options = options.Value;
 
        if (string.IsNullOrWhiteSpace(_options.Key) ||
            Encoding.UTF8.GetByteCount(_options.Key) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key must be configured and at least 32 bytes long.");
        }
    }
 
    public (string Token, DateTime ExpiresAt) CreateAccessToken(
        ApplicationUser user, IList<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
 
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
 
            // Unique per token
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };
 

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
 
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
 
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);
 
        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
 
    public (string RawToken, string TokenHash) CreateRefreshToken()
    {
       
        var bytes = RandomNumberGenerator.GetBytes(64);
        var raw = Base64UrlEncode(bytes);
        return (raw, HashToken(raw));
    }
 

    public string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
 
    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
               .TrimEnd('=')
               .Replace('+', '-')
               .Replace('/', '_');
}