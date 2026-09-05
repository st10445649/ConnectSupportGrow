using System.Net;
using System.Text.Json;
using ConnectGrow.Models;

namespace ConnectGrow.Services;

public abstract class ApiClientBase
{
    protected readonly HttpClient Http;
    private readonly ILogger _logger;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected ApiClientBase(HttpClient http, ILogger logger)
    {
        Http = http;
        _logger = logger;
    }

    protected async Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
    {
        try
        {
            using var response = await Http.GetAsync(path, ct);
            return await ReadAsync<T>(response, ct);
        }
        catch (Exception ex)
        {
            return Unreachable<T>(ex, "GET", path);
        }
    }

    protected async Task<ApiResult<TResponse>> PostAsync<TResponse>(
        string path, object? body, CancellationToken ct = default)
    {
        try
        {
            using var response = await Http.PostAsJsonAsync(path, body, JsonOptions, ct);
            return await ReadAsync<TResponse>(response, ct);
        }
        catch (Exception ex)
        {
            return Unreachable<TResponse>(ex, "POST", path);
        }
    }

    //For endpoints that return 204 with no body
    protected async Task<ApiResult> PostAsync(
        string path, object? body, CancellationToken ct = default)
    {
        try
        {
            using var response = await Http.PostAsJsonAsync(path, body, JsonOptions, ct);

            return response.IsSuccessStatusCode
                ? ApiResult.Success((int)response.StatusCode)
                : ApiResult.Failure((int)response.StatusCode, await ExtractErrorAsync(response, ct));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST {Path} failed.", path);
            return ApiResult.Failure(503, FriendlyUnreachableMessage);
        }
    }

    protected async Task<ApiResult<T>> PutAsync<T>(
        string path, object? body, CancellationToken ct = default)
    {
        try
        {
            using var response = await Http.PutAsJsonAsync(path, body, JsonOptions, ct);
            return await ReadAsync<T>(response, ct);
        }
        catch (Exception ex)
        {
            return Unreachable<T>(ex, "PUT", path);
        }
    }

    private async Task<ApiResult<T>> ReadAsync<T>(
        HttpResponseMessage response, CancellationToken ct)
    {
        var status = (int)response.StatusCode;

        if (!response.IsSuccessStatusCode)
            return ApiResult<T>.Failure(status, await ExtractErrorAsync(response, ct));

        if (response.StatusCode == HttpStatusCode.NoContent)
            return ApiResult<T>.Success(default!, status);

        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);

        return value is null
            ? ApiResult<T>.Failure(status, "The server returned an empty response.")
            : ApiResult<T>.Success(value, status);
    }

    //Pulls the message out of the API's error body. Falls back to a generic
    // line rather than surfacing raw response text, which could contain
    // internal detail
    private static async Task<string> ExtractErrorAsync(
        HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorModel>(JsonOptions, ct);

            if (!string.IsNullOrWhiteSpace(error?.Message))
                return error.Message;
        }
        catch
        {
            
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Please sign in to continue.",
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "We could not find what you were looking for.",
            HttpStatusCode.Conflict => "That action conflicts with the current state.",
            HttpStatusCode.TooManyRequests => "Too many attempts. Please wait a moment.",
            _ => "Something went wrong. Please try again."
        };
    }

    private ApiResult<T> Unreachable<T>(Exception ex, string method, string path)
    {
        _logger.LogError(ex, "{Method} {Path} failed.", method, path);
        return ApiResult<T>.Failure(503, FriendlyUnreachableMessage);
    }
    private const string FriendlyUnreachableMessage =
        "We could not reach the server. Please check your connection and try again.";
}