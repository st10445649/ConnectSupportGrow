using System.Security.Claims;
using ConnectGrow.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ConnectGrow.Services;

public interface ISignInService
{
    //adds the client's auhtentication cookie from the API auth response
    //carries the access and refresh tokens as claims
    Task SignInAsync(HttpContext context, AuthResponseModel auth, bool refreshOnly = false);

    Task SignOutAsync(HttpContext context);
}

public class SignInService : ISignInService
{
    public const string SchemeName = CookieAuthenticationDefaults.AuthenticationScheme;

    public async Task SignInAsync(
        HttpContext context, AuthResponseModel auth, bool refreshOnly = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.User.Id.ToString()),
            new(ClaimTypes.Email, auth.User.Email),
            new(ClaimTypes.Name, auth.User.FullName),
            new(ClaimTypes.GivenName, auth.User.FirstName),

            // Carried so the bearer handler and refresh middleware can read them
            new(TokenClaims.AccessToken, auth.AccessToken),
            new(TokenClaims.RefreshToken, auth.RefreshToken),
            new(TokenClaims.AccessTokenExpiresAt,
                new DateTimeOffset(auth.AccessTokenExpiresAt, TimeSpan.Zero)
                    .ToUnixTimeSeconds().ToString())
        };

        // Mirrored from the API so [Authorize(Roles = "Admin")] works on the
        // client too. The client's check is for navigation and hiding controls;
        // the API remains the actual checkpoint
        claims.AddRange(auth.User.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);

        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
            AllowRefresh = true
        };

        await context.SignInAsync(SchemeName, principal, properties);

        if (refreshOnly)
        {
            context.User = principal;
        }
    }

    public Task SignOutAsync(HttpContext context) => context.SignOutAsync(SchemeName);
}