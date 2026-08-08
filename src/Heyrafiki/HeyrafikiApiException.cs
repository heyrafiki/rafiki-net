using System.Net;

namespace Heyrafiki;

/// <summary>Represents a structured error returned by the Heyrafiki API.</summary>
public sealed class HeyrafikiApiException : Exception
{
    internal HeyrafikiApiException(
        HttpStatusCode statusCode,
        string code,
        string message,
        string? requestId,
        string? docs,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Code = code;
        RequestId = requestId;
        Docs = docs;
    }

    /// <summary>Gets the HTTP status returned by the API.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>Gets the stable API error code.</summary>
    public string Code { get; }

    /// <summary>Gets the request identifier used for support and tracing.</summary>
    public string? RequestId { get; }

    /// <summary>Gets the related documentation URL, when supplied.</summary>
    public string? Docs { get; }
}
