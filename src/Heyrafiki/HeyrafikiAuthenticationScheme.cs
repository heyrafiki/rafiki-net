namespace Heyrafiki;

/// <summary>Controls how the API key is sent.</summary>
public enum HeyrafikiAuthenticationScheme
{
    /// <summary>Sends the key as a Bearer credential.</summary>
    Bearer,

    /// <summary>Sends the key in the x-api-key header.</summary>
    ApiKeyHeader,
}
