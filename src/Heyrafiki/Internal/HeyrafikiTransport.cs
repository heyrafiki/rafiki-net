using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Heyrafiki.Internal;

internal sealed class HeyrafikiTransport
{
    private const string UserAgent = "heyrafiki-dotnet/0.1.0-beta.1";
    private static readonly Random RetryJitter = new();
    private readonly HttpClient _httpClient;
    private readonly HeyrafikiClientOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    internal HeyrafikiTransport(HttpClient httpClient, HeyrafikiClientOptions options)
    {
        _httpClient = httpClient;
        _options = options;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        };
    }

    internal Task<T> GetAsync<T>(string path, CancellationToken cancellationToken) =>
        SendAsync<T>(HttpMethod.Get, path, null, null, retryEligible: true, cancellationToken);

    internal Task<T> PostAsync<T>(
        string path,
        object? body,
        string? idempotencyKey,
        IReadOnlyDictionary<string, string>? additionalHeaders,
        bool retryEligible,
        CancellationToken cancellationToken) =>
        SendAsync<T>(HttpMethod.Post, path, body, BuildHeaders(idempotencyKey, additionalHeaders), retryEligible, cancellationToken);

    internal Task<T> DeleteAsync<T>(string path, CancellationToken cancellationToken) =>
        SendAsync<T>(HttpMethod.Delete, path, null, null, retryEligible: false, cancellationToken);

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        IReadOnlyDictionary<string, string>? headers,
        bool retryEligible,
        CancellationToken cancellationToken)
    {
        var serializedBody = body is null ? null : JsonSerializer.Serialize(body, body.GetType(), _jsonOptions);

        for (var attempt = 0; ; attempt++)
        {
            using var request = CreateRequest(method, path, serializedBody, headers);
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (retryEligible && attempt < _options.MaxRetries && IsRetryable(response.StatusCode))
            {
                var delay = GetRetryDelay(response, attempt);
                response.Dispose();
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                continue;
            }

            using (response)
            {
#if NETSTANDARD2_0
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#else
                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#endif
                if (!response.IsSuccessStatusCode)
                {
                    throw CreateApiException(response, content);
                }

                try
                {
                    var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                    if (result is null) throw new JsonException("The response body was null.");
                    return result;
                }
                catch (JsonException exception)
                {
                    throw new HeyrafikiApiException(
                        response.StatusCode,
                        "invalid_response",
                        "The Heyrafiki API returned an invalid response.",
                        GetHeader(response, "X-Request-Id"),
                        null,
                        exception);
                }
            }
        }
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string path,
        string? serializedBody,
        IReadOnlyDictionary<string, string>? headers)
    {
        var request = new HttpRequestMessage(method, BuildUri(path));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);

        if (_options.AuthenticationScheme == HeyrafikiAuthenticationScheme.Bearer)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        else
            request.Headers.TryAddWithoutValidation("x-api-key", _options.ApiKey);

        if (serializedBody is not null)
            request.Content = new StringContent(serializedBody, Encoding.UTF8, "application/json");

        if (headers is not null)
            foreach (var header in headers) request.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return request;
    }

    private Uri BuildUri(string path)
    {
        var baseAddress = _options.BaseAddress.AbsoluteUri.TrimEnd('/');
        return new Uri(baseAddress + "/" + path.TrimStart('/'), UriKind.Absolute);
    }

    private static Dictionary<string, string>? BuildHeaders(
        string? idempotencyKey,
        IReadOnlyDictionary<string, string>? additionalHeaders)
    {
        if (idempotencyKey is null && additionalHeaders is null) return null;
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (idempotencyKey is not null) headers["Idempotency-Key"] = idempotencyKey;
        if (additionalHeaders is not null)
            foreach (var header in additionalHeaders) headers[header.Key] = header.Value;
        return headers;
    }

    private static bool IsRetryable(HttpStatusCode statusCode) =>
        (int)statusCode is 429 or 503;

    private TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta) return CapDelay(delta);
        if (retryAfter?.Date is { } date) return CapDelay(date - DateTimeOffset.UtcNow);

        var exponential = _options.InitialRetryDelay.TotalMilliseconds * Math.Pow(2, attempt);
        double jitterFactor;
        lock (RetryJitter) jitterFactor = RetryJitter.NextDouble();
        var jitter = exponential * 0.2 * jitterFactor;
        return CapDelay(TimeSpan.FromMilliseconds(exponential + jitter));
    }

    private TimeSpan CapDelay(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero) return TimeSpan.Zero;
        return delay > _options.MaximumRetryDelay ? _options.MaximumRetryDelay : delay;
    }

    private static HeyrafikiApiException CreateApiException(HttpResponseMessage response, string content)
    {
        string code = "request_failed";
        string message = "The Heyrafiki API request failed.";
        string? docs = null;

        try
        {
            var apiError = JsonSerializer.Deserialize<ApiErrorEnvelope>(content)?.Error;
            if (apiError is not null)
            {
                if (!string.IsNullOrWhiteSpace(apiError.Code)) code = apiError.Code ?? code;
                if (!string.IsNullOrWhiteSpace(apiError.Message)) message = apiError.Message ?? message;
                if (!string.IsNullOrWhiteSpace(apiError.Docs)) docs = apiError.Docs;
            }
        }
        catch (JsonException)
        {
        }

        return new HeyrafikiApiException(
            response.StatusCode,
            code,
            message,
            GetHeader(response, "X-Request-Id"),
            docs);
    }

    private static string? GetHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

    private sealed class ApiErrorEnvelope
    {
        [JsonPropertyName("error")]
        public ApiError? Error { get; set; }
    }

    private sealed class ApiError
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("docs")]
        public string? Docs { get; set; }
    }
}
