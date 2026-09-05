using System.Security.Claims;

namespace ConnectGrow.Services;

/* Refreshes the API access token before an action runs.
refreshing mean s writing a new auth cookie, and a
cookie is a response header — once the views (Razor) starts streaming the response,
headers are already sent. Therfore the refresh happens here,
early in the pipeline, rather than reactively inside the HTTP handler */
public class TokenRefreshMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenRefreshMiddleware> _logger;
    private static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(10);

    public TokenRefreshMiddleware(RequestDelegate next, ILogger<TokenRefreshMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAuthApiClient authApi,
        ISignInService signIn)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        if (ShouldRefresh(context.User))
        {
            var refreshToken = context.User.FindFirstValue(TokenClaims.RefreshToken);

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                await signIn.SignOutAsync(context);
            }
            else
            {
                var result = await authApi.RefreshAsync(refreshToken, context.RequestAborted);

                if (result.IsSuccess && result.Value is not null)
                {
                    await signIn.SignInAsync(context, result.Value, refreshOnly: true);

                    _logger.LogDebug("Access token refreshed for the current session.");
                }
                else
                {
                    // The refresh token is spent, revoked or expired. Signs the user out
                    _logger.LogInformation(
                        "Token refresh failed ({Status}). Signing the user out.",
                        result.StatusCode);

                    await signIn.SignOutAsync(context);
                }
            }
        }

        await _next(context);
    }

    private static bool ShouldRefresh(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(TokenClaims.AccessTokenExpiresAt);

        // No expiry claim means a cookie from an older format. Read as an expired/stale token
        if (!long.TryParse(raw, out var unixSeconds)) return true;

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);

        return expiresAt - DateTimeOffset.UtcNow < RefreshWindow;
    }
}

public static class TokenRefreshMiddlewareExtensions
{
    //Must be registered after UseAuthentication (it reads the signed-in user) and before UseAuthorization and the endpoints.
    public static IApplicationBuilder UseTokenRefresh(this IApplicationBuilder app) =>
        app.UseMiddleware<TokenRefreshMiddleware>();
}