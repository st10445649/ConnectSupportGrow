namespace ConnectGrow.Services;
public class ApiOptions
{
    public const string SectionName = "Api";
 
    //base address
    public string BaseUrl { get; set; } = "http://localhost:5212";
 
    public int TimeoutSeconds { get; set; } = 30;
}
 

public static class TokenClaims
{
    public const string AccessToken = "csg:access_token";
    public const string RefreshToken = "csg:refresh_token";
    public const string AccessTokenExpiresAt = "csg:access_expires";
}
 
// Outcome of an API call. Controllers branch on this rather than catching
//exceptions, so a 409 from the API becomes a validation message on the form
//instead of a error 500 page
public class ApiResult
{
    protected ApiResult(bool isSuccess, int statusCode, string? error)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Error = error;
    }
 
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public int StatusCode { get; }
    public string? Error { get; }
 
    //If user's session has lapsed and they need to sign in again
    public bool IsUnauthorised => StatusCode == 401;
 
    public bool IsForbidden => StatusCode == 403;
    public bool IsNotFound => StatusCode == 404;
 
    public static ApiResult Success(int statusCode = 200) => new(true, statusCode, null);
    public static ApiResult Failure(int statusCode, string error) => new(false, statusCode, error);
}
 
public class ApiResult<T> : ApiResult
{
    private ApiResult(T? value, bool isSuccess, int statusCode, string? error)
        : base(isSuccess, statusCode, error) => Value = value;
 
    public T? Value { get; }
 
    public static ApiResult<T> Success(T value, int statusCode = 200) =>
        new(value, true, statusCode, null);
 
    public static new ApiResult<T> Failure(int statusCode, string error) =>
        new(default, false, statusCode, error);
}
 