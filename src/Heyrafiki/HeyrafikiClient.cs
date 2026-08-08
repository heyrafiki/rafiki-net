using Heyrafiki.Internal;

namespace Heyrafiki;

/// <summary>Provides typed access to the Heyrafiki API.</summary>
public sealed class HeyrafikiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    /// <summary>Initializes a Heyrafiki API client.</summary>
    public HeyrafikiClient(HeyrafikiClientOptions options, HttpClient? httpClient = null)
    {
#if NETSTANDARD2_0
        if (options is null) throw new ArgumentNullException(nameof(options));
#else
        ArgumentNullException.ThrowIfNull(options);
#endif
        var snapshot = ValidateAndCopy(options);
        _ownsHttpClient = httpClient is null;
        _httpClient = httpClient ?? new HttpClient();
        var transport = new HeyrafikiTransport(_httpClient, snapshot);

        Api = new ApiClient(transport);
        Practitioners = new PractitionersClient(transport);
        Bookings = new BookingsClient(transport);
        Sessions = new SessionsClient(transport);
        EligibilityChecks = new EligibilityChecksClient(transport);
        Coverages = new CoveragesClient(transport);
        CoverageBatches = new CoverageBatchesClient(transport);
        Preauthorizations = new PreauthorizationsClient(transport);
        Claims = new ClaimsClient(transport);
        Remittances = new RemittancesClient(transport);
        WebhookEndpoints = new WebhookEndpointsClient(transport);
    }

    /// <summary>Gets API metadata operations.</summary>
    public ApiClient Api { get; }

    /// <summary>Gets Practitioner operations.</summary>
    public PractitionersClient Practitioners { get; }

    /// <summary>Gets Booking operations.</summary>
    public BookingsClient Bookings { get; }

    /// <summary>Gets Session operations.</summary>
    public SessionsClient Sessions { get; }

    /// <summary>Gets eligibility-check operations.</summary>
    public EligibilityChecksClient EligibilityChecks { get; }

    /// <summary>Gets Coverage observation operations.</summary>
    public CoveragesClient Coverages { get; }

    /// <summary>Gets Coverage batch operations.</summary>
    public CoverageBatchesClient CoverageBatches { get; }

    /// <summary>Gets pre-authorization operations.</summary>
    public PreauthorizationsClient Preauthorizations { get; }

    /// <summary>Gets Claim operations.</summary>
    public ClaimsClient Claims { get; }

    /// <summary>Gets remittance operations.</summary>
    public RemittancesClient Remittances { get; }

    /// <summary>Gets Webhook endpoint operations.</summary>
    public WebhookEndpointsClient WebhookEndpoints { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttpClient) _httpClient.Dispose();
    }

    private static HeyrafikiClientOptions ValidateAndCopy(HeyrafikiClientOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new ArgumentException("ApiKey is required.", nameof(options));
        if (!options.BaseAddress.IsAbsoluteUri || options.BaseAddress.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("BaseAddress must be an absolute HTTPS URI.", nameof(options));
        if (options.MaxRetries is < 0 or > 10)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxRetries must be between 0 and 10.");
        if (options.InitialRetryDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), "InitialRetryDelay cannot be negative.");
        if (options.MaximumRetryDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), "MaximumRetryDelay cannot be negative.");

        return new HeyrafikiClientOptions
        {
            ApiKey = options.ApiKey,
            BaseAddress = options.BaseAddress,
            AuthenticationScheme = options.AuthenticationScheme,
            MaxRetries = options.MaxRetries,
            InitialRetryDelay = options.InitialRetryDelay,
            MaximumRetryDelay = options.MaximumRetryDelay,
        };
    }
}
