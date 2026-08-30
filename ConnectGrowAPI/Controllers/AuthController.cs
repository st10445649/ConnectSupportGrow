
using ConnectGrowAPI.Controllers;
using ConnectGrowAPI.Dtos;
using ConnectGrowAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ConnectGrowAPI.Api.Controllers;

//auth controller responsible for authorisation and user management handling of requests
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    public const string AccessTokenCookie = "csg.access";
    public const string RefreshTokenCookie = "csg.refresh";

    private readonly IAuthService _auth;
    private readonly JwtOptions _jwt;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService auth, IOptions<JwtOptions> jwt, IWebHostEnvironment env)
    {
        _auth = auth;
        _jwt = jwt.Value;
        _env = env;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(request, ClientIp(), ct);
        if (result.IsFailure) return Problem(result);

        WriteAuthCookies(result.Value!);
        return Ok(result.Value);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, ClientIp(), ct);
        if (result.IsFailure) return Problem(result);

        WriteAuthCookies(result.Value!);
        return Ok(result.Value);
    }

    //Exchanges a refresh token for a new access token. The old refresh token is
    // rotated out; presenting it again revokes every session for that user.
   
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshTokenRequest? request, CancellationToken ct)
    {
        var token = Request.Cookies[RefreshTokenCookie] ?? request?.RefreshToken;

        var result = await _auth.RefreshAsync(token ?? string.Empty, ClientIp(), ct);

        if (result.IsFailure)
        {
            ClearAuthCookies();
            return Problem(result);
        }

        WriteAuthCookies(result.Value!);
        return Ok(result.Value);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var token = Request.Cookies[RefreshTokenCookie];
        await _auth.LogoutAsync(token, CurrentUserId, ct);

        ClearAuthCookies();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileDto>> Me(CancellationToken ct)
    {
        var result = await _auth.GetProfileAsync(CurrentUserId, ct);
        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile(
        [FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var result = await _auth.UpdateProfileAsync(CurrentUserId, request, ct);
        return result.IsSuccess ? Ok(result.Value) : Problem(result);
    }

    //changing a password revokes every other active session on another device
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var result = await _auth.ChangePasswordAsync(CurrentUserId, request, ct);
        if (result.IsFailure) return Problem(result);

        ClearAuthCookies();
        return NoContent();
    }

    //returns 200, whether or not the address has an account. Reporting
    /// "no user registered" would let anyone test which email addresses are registered.

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await _auth.CreatePasswordResetTokenAsync(request.Email, ct);

        const string message =
            "If an account exists for that email address, a reset link has been sent.";

        //JUST temp to see if token returns as email connection hasn't been added yet.
        if (_env.IsDevelopment() && result.IsSuccess && result.Value is not null)
            return Ok(new { message, devResetToken = result.Value });

        return Ok(new { message });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _auth.ResetPasswordAsync(request, ct);
        return result.IsSuccess
            ? Ok(new { message = "Your password has been reset. You can now sign in." })
            : Problem(result);
    }

// https://np4652.medium.com/creating-cookie-based-authentication-and-authorization-in-net-core-4aa25f1e845a
// https://www.c-sharpcorner.com/article/authentication-and-authorization-in-asp-net-core-mvc-using-cookie/  
//https://www.aspsnippets.com/Articles/5398/How-to-Set-and-Get-Cookies-in-ASPNet-MVC/ 
    private void WriteAuthCookies(AuthResponse auth)
    {
        Response.Cookies.Append(AccessTokenCookie, auth.AccessToken, BuildCookieOptions(
            expires: auth.AccessTokenExpiresAt,
            path: "/"));

        Response.Cookies.Append(RefreshTokenCookie, auth.RefreshToken, BuildCookieOptions(
            expires: DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays),
            path: "/api/auth"));
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete(AccessTokenCookie, BuildCookieOptions(DateTime.UtcNow, "/"));
        Response.Cookies.Delete(RefreshTokenCookie, BuildCookieOptions(DateTime.UtcNow, "/api/auth"));
    }

// https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.cookieoptions?view=aspnetcore-10.0
    private CookieOptions BuildCookieOptions(DateTime expires, string path) => new()
    {
        HttpOnly = true,                        // unreadable from JavaScript
        Secure = !_env.IsDevelopment(),         // HTTPS only outside local dev
        SameSite = SameSiteMode.Lax,            
        Expires = expires,
        Path = path,
        Domain = string.IsNullOrWhiteSpace(_jwt.CookieDomain) ? null : _jwt.CookieDomain
    };

    private string? ClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();
}