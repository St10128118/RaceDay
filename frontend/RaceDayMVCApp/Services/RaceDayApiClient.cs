using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDayMVCApp.Models;

namespace RaceDayMVCApp.Services;

public interface IRaceDayApiClient
{
    Task<ApiResponse<T>> GetAsync<T>(string path, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> GetWithTokenAsync<T>(string path, string token, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> PostAsync<T>(string path, object body, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> PutAsync<T>(string path, object body, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> DeleteAsync<T>(string path, CancellationToken cancellationToken = default);
}

public sealed class RaceDayApiClient(
    HttpClient httpClient,
    ILogger<RaceDayApiClient> logger) : IRaceDayApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResponse<T>> GetAsync<T>(string path, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, null, cancellationToken);

    public Task<ApiResponse<T>> GetWithTokenAsync<T>(
        string path,
        string token,
        CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, token, cancellationToken);

    public Task<ApiResponse<T>> PostAsync<T>(
        string path,
        object body,
        CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Post, path, body, null, cancellationToken);

    public Task<ApiResponse<T>> PutAsync<T>(
        string path,
        object body,
        CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Put, path, body, null, cancellationToken);

    public Task<ApiResponse<T>> DeleteAsync<T>(
        string path,
        CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Delete, path, null, null, cancellationToken);

    private async Task<ApiResponse<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        string? bearerToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorAsync(response, cancellationToken);
                return new ApiResponse<T>(false, default, error, (int)response.StatusCode);
            }

            if (response.StatusCode == HttpStatusCode.NoContent ||
                response.Content.Headers.ContentLength == 0)
                return new ApiResponse<T>(true, default, null, (int)response.StatusCode);

            try
            {
                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
                return new ApiResponse<T>(true, data, null, (int)response.StatusCode);
            }
            catch (JsonException exception)
            {
                logger.LogError(exception, "RaceDay API returned invalid JSON for {Method} {Path}.", method, path);
                return new ApiResponse<T>(false, default, "The RaceDay service returned an unexpected response.", 502);
            }
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "RaceDay API request failed for {Method} {Path}.", method, path);
            return new ApiResponse<T>(false, default, "The RaceDay service could not be reached. Check the API address and try again.", 503);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "RaceDay API request timed out for {Method} {Path}.", method, path);
            return new ApiResponse<T>(false, default, "The RaceDay service took too long to respond. Please try again.", 504);
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
            return FriendlyStatusMessage(response.StatusCode);

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                return message.GetString()!;
            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                return detail.GetString()!;
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(property => property.Value.ValueKind == JsonValueKind.Array
                        ? property.Value.EnumerateArray().Select(value => value.GetString()).Where(value => !string.IsNullOrWhiteSpace(value))
                        : [])
                    .ToArray();
                if (messages.Length > 0) return string.Join(" ", messages);
            }
        }
        catch (JsonException)
        {
            return FriendlyStatusMessage(response.StatusCode);
        }

        return FriendlyStatusMessage(response.StatusCode);
    }

    private static string FriendlyStatusMessage(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest => "Please check the information and try again.",
        HttpStatusCode.Unauthorized => "Your sign-in has expired. Please sign in again.",
        HttpStatusCode.Forbidden => "You do not have permission to do that.",
        HttpStatusCode.NotFound => "That RaceDay item could not be found.",
        HttpStatusCode.Conflict => "That action conflicts with the current event or enrolment status.",
        _ => "The RaceDay service could not complete the request. Please try again."
    };
}
