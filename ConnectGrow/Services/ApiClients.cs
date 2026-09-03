using ConnectGrow.Models;
namespace ConnectGrow.Services;


/* ApiClient, BaseApiClient, and ApiClientOptions work together using the Options Pattern and 
Dependency Injection (DI). This pattern centralises base configurations (URLs, timeouts, auth headers) 
while keeping individual API callers clean and testable.

https://www.c-sharpcorner.com/article/understanding-the-options-pattern-in-asp-net-core-with-a-practical-example
https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-10.0 
 */
 public interface IAuthApiClient
{
    Task<ApiResult<AuthResponseModel>> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<ApiResult<AuthResponseModel>> RegisterAsync(RegisterInputModel input, CancellationToken ct = default);
    Task<ApiResult<AuthResponseModel>> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task<ApiResult<Users>> GetProfileAsync(CancellationToken ct = default);
    Task<ApiResult> LogoutAsync(CancellationToken ct = default);
}

//Auth
public class AuthApiClient : ApiClientBase, IAuthApiClient
{
   
    public const string HttpClientName = "csg-api-anonymous";

    public AuthApiClient(HttpClient http, ILogger<AuthApiClient> logger)
        : base(http, logger) { }

    public Task<ApiResult<AuthResponseModel>> LoginAsync(
        string email, string password, CancellationToken ct = default) =>
        PostAsync<AuthResponseModel>("/api/auth/login", new { email, password }, ct);

    public Task<ApiResult<AuthResponseModel>> RegisterAsync(
        RegisterInputModel input, CancellationToken ct = default) =>
        PostAsync<AuthResponseModel>("/api/auth/register", new
        {
            firstName = input.FirstName,
            lastName = input.LastName,
            email = input.Email,
            password = input.Password,
            confirmPassword = input.ConfirmPassword,
            organisation = input.Organisation
        }, ct);

    public Task<ApiResult<AuthResponseModel>> RefreshAsync(
        string refreshToken, CancellationToken ct = default) =>
        PostAsync<AuthResponseModel>("/api/auth/refresh", new { refreshToken }, ct);

    public Task<ApiResult<Users>> GetProfileAsync(CancellationToken ct = default) =>
        GetAsync<Users>("/api/auth/me", ct);

    public Task<ApiResult> LogoutAsync(CancellationToken ct = default) =>
        PostAsync("/api/auth/logout", null, ct);
}

// Webinars
public interface IWebinarApiClient
{
    Task<ApiResult<List<Webinars>>> GetCatalogueAsync(string? category = null, CancellationToken ct = default);
    Task<ApiResult<WebinarDetailModel>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ApiResult<List<object>>> GetCalendarFeedAsync(CancellationToken ct = default);
}

public class WebinarApiClient : ApiClientBase, IWebinarApiClient
{
    public WebinarApiClient(HttpClient http, ILogger<WebinarApiClient> logger)
        : base(http, logger) { }

    public Task<ApiResult<List<Webinars>>> GetCatalogueAsync(
        string? category = null, CancellationToken ct = default)
    {
        var path = string.IsNullOrWhiteSpace(category)
            ? "/api/webinars"
            : $"/api/webinars?category={Uri.EscapeDataString(category)}";

        return GetAsync<List<Webinars>>(path, ct);
    }

    public Task<ApiResult<WebinarDetailModel>> GetByIdAsync(int id, CancellationToken ct = default) =>
        GetAsync<WebinarDetailModel>($"/api/webinars/{id}", ct);

    public Task<ApiResult<List<object>>> GetCalendarFeedAsync(CancellationToken ct = default) =>
        GetAsync<List<object>>("/api/webinars/calendar", ct);
}


// Bookings

public interface IBookingApiClient
{
    Task<ApiResult<List<Bookings>>> GetMineAsync(CancellationToken ct = default);
    Task<ApiResult<Bookings>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ApiResult<BookingCreatedModel>> CreateAsync(int webinarId, CancellationToken ct = default);
    Task<ApiResult> CancelAsync(int id, CancellationToken ct = default);
}

public class BookingApiClient : ApiClientBase, IBookingApiClient
{
    public BookingApiClient(HttpClient http, ILogger<BookingApiClient> logger)
        : base(http, logger) { }

    public Task<ApiResult<List<Bookings>>> GetMineAsync(CancellationToken ct = default) =>
        GetAsync<List<Bookings>>("/api/bookings", ct);

    public Task<ApiResult<Bookings>> GetByIdAsync(int id, CancellationToken ct = default) =>
        GetAsync<Bookings>($"/api/bookings/{id}", ct);

    public Task<ApiResult<BookingCreatedModel>> CreateAsync(
        int webinarId, CancellationToken ct = default) =>
        PostAsync<BookingCreatedModel>("/api/bookings", new { webinarId }, ct);

    public Task<ApiResult> CancelAsync(int id, CancellationToken ct = default) =>
        PostAsync($"/api/bookings/{id}/cancel", null, ct);
}