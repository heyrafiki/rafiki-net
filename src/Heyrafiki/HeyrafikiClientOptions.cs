namespace Heyrafiki;

/// <summary>Configures a Heyrafiki API client.</summary>
public sealed class HeyrafikiClientOptions
{
    /// <summary>Gets or sets the server-side Sandbox or production secret key.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the API base address.</summary>
    public Uri BaseAddress { get; set; } = new("https://api.heyrafiki.space/v1");

    /// <summary>Gets or sets the authentication header format.</summary>
    public HeyrafikiAuthenticationScheme AuthenticationScheme { get; set; } = HeyrafikiAuthenticationScheme.Bearer;

    /// <summary>Gets or sets the number of retries after the first request.</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>Gets or sets the initial delay for exponential retry backoff.</summary>
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMilliseconds(250);

    /// <summary>Gets or sets the maximum delay between attempts.</summary>
    public TimeSpan MaximumRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
}
