using System.Net.Http.Headers;
using System.Security.Claims;

namespace ConnectGrow.Services;


/* 
This service helps to attach the API access token to every outgoing request in the Http execution pipeline.
It is an outgoing request interceptior that attaches the user's token to all outgoing request for the API.
It checks whether the API rejected the request as Unauthorized 
(meaning both access and refresh tokens have expired/revoked).
 */

 //https://learn.microsoft.com/en-us/dotnet/api/system.net.http.delegatinghandler?view=net-10.0
 //https://www.c-sharpcorner.com/article/exploring-delegating-handlers-in-c-sharp-net 

public class BearerTokenHandler : DelegatingHandler
{
    // Gives access to the current HTTP request context in ASP.NET Core (cookies, User principal, claims)
    private readonly IHttpContextAccessor _accessor;

    //Logs security events 
    private readonly ILogger<BearerTokenHandler> _logger;

    public BearerTokenHandler(
        IHttpContextAccessor accessor,
        ILogger<BearerTokenHandler> logger)
    {
        _accessor = accessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {

        // Access the current user's claims principal through HttpContext.
        // the token was stored in a custom claim when the user signed in.
        var token = _accessor.HttpContext?.User?.FindFirstValue(TokenClaims.AccessToken);

        // If a authenticated user exists and has a token claim, this formats it into a standard HTTP Header    
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        //Passes request 
        var response = await base.SendAsync(request, cancellationToken);


        // If the backend API responds with 401 Unauthorized then log error. 
        //Because the token refresh in the other sevrice runs earlier prior to rendering a view, if 
        // a 401 here implies both the Access Token and Refresh Token are dead, revoked, or invalid.
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            
            _logger.LogInformation(
                "API returned 401 for {Path}; the session is no longer valid.",
                request.RequestUri?.AbsolutePath);
        }

// Sends the HttpResponseMessage back to ApiClientBase to be parsed into ApiResult
        return response;
    }
}