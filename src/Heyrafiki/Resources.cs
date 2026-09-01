using Heyrafiki.Internal;
using Heyrafiki.Models;

namespace Heyrafiki;

/// <summary>API metadata operations.</summary>
public sealed class ApiClient
{
    private readonly HeyrafikiTransport _transport;
    internal ApiClient(HeyrafikiTransport transport) => _transport = transport;

    /// <summary>Retrieves API metadata for the authenticated project.</summary>
    public Task<ApiInformation> RetrieveAsync(CancellationToken cancellationToken = default) =>
        _transport.GetAsync<ApiInformation>("/", cancellationToken);
}

/// <summary>Practitioner operations.</summary>
public sealed class PractitionersClient
{
    private readonly HeyrafikiTransport _transport;
    internal PractitionersClient(HeyrafikiTransport transport) => _transport = transport;

    /// <summary>Lists synthetic Practitioner profiles.</summary>
    public Task<PractitionerList> ListAsync(int limit = 20, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<PractitionerList>($"/practitioners?limit={Guard.Limit(limit)}", cancellationToken);

    /// <summary>Retrieves a synthetic Practitioner profile.</summary>
    public Task<Practitioner> RetrieveAsync(string practitionerId, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<Practitioner>($"/practitioners/{Guard.Escape(Guard.Identifier(practitionerId, "prc_", nameof(practitionerId)))}", cancellationToken);

    /// <summary>Retrieves recurring weekly availability.</summary>
    public Task<PractitionerAvailability> RetrieveAvailabilityAsync(string practitionerId, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<PractitionerAvailability>($"/practitioners/{Guard.Escape(Guard.Identifier(practitionerId, "prc_", nameof(practitionerId)))}/availability", cancellationToken);
}

/// <summary>Booking operations.</summary>
public sealed class BookingsClient
{
    private readonly HeyrafikiTransport _transport;
    internal BookingsClient(HeyrafikiTransport transport) => _transport = transport;

    /// <summary>Lists tenant-scoped synthetic Bookings.</summary>
    public Task<BookingList> ListAsync(int limit = 20, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<BookingList>($"/bookings?limit={Guard.Limit(limit)}", cancellationToken);

    /// <summary>Retrieves a synthetic Booking.</summary>
    public Task<Booking> RetrieveAsync(string bookingId, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<Booking>($"/bookings/{Guard.Escape(Guard.Identifier(bookingId, "bkg_", nameof(bookingId)))}", cancellationToken);

    /// <summary>Creates a synthetic Booking and Session.</summary>
    public Task<Booking> CreateAsync(BookingInput input, string idempotencyKey, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<Booking>("/bookings", Guard.Input(input, nameof(input)), Guard.IdempotencyKey(idempotencyKey), null, true, cancellationToken);
}

/// <summary>Session operations.</summary>
public sealed class SessionsClient
{
    private readonly HeyrafikiTransport _transport;
    internal SessionsClient(HeyrafikiTransport transport) => _transport = transport;

    /// <summary>Lists synthetic Sessions without Person or clinical identifiers.</summary>
    public Task<SessionList> ListAsync(int limit = 20, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<SessionList>($"/sessions?limit={Guard.Limit(limit)}", cancellationToken);

    /// <summary>Retrieves a synthetic Session.</summary>
    public Task<Session> RetrieveAsync(string sessionId, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<Session>($"/sessions/{Guard.Escape(Guard.Identifier(sessionId, "ses_", nameof(sessionId)))}", cancellationToken);
}

/// <summary>Benefit eligibility operations.</summary>
public sealed class EligibilityChecksClient
{
    private readonly HeyrafikiTransport _transport;
    internal EligibilityChecksClient(HeyrafikiTransport transport) => _transport = transport;

    /// <summary>Checks a synthetic Benefit for a scheduled service.</summary>
    public Task<EligibilityCheck> CreateAsync(EligibilityCheckInput input, string idempotencyKey, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<EligibilityCheck>("/eligibility_checks", Guard.Input(input, nameof(input)), Guard.IdempotencyKey(idempotencyKey), null, true, cancellationToken);

    /// <summary>Retrieves a synthetic eligibility decision.</summary>
    public Task<EligibilityCheck> RetrieveAsync(string eligibilityCheckId, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<EligibilityCheck>($"/eligibility_checks/{Guard.Escape(Guard.Identifier(eligibilityCheckId, "elig_", nameof(eligibilityCheckId)))}", cancellationToken);
}

/// <summary>Coverage observation operations.</summary>
public sealed class CoveragesClient
{
    private readonly HeyrafikiTransport _transport;
    internal CoveragesClient(HeyrafikiTransport transport) => _transport = transport;

    /// <summary>Records an authoritative Coverage observation.</summary>
    public Task<CoverageObservation> RecordAsync(CoverageObservationInput input, string idempotencyKey, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<CoverageObservation>("/coverages", Guard.Input(input, nameof(input)), Guard.IdempotencyKey(idempotencyKey), null, true, cancellationToken);
}

/// <summary>Coverage batch operations.</summary>
public sealed class CoverageBatchesClient
{
    private readonly HeyrafikiTransport _transport;
    internal CoverageBatchesClient(HeyrafikiTransport transport) => _transport = transport;

    /// <summary>Records an authoritative Coverage batch.</summary>
    public Task<CoverageBatchResult> RecordAsync(
        CoverageBatchInput input,
        string artifactReference,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        _transport.PostAsync<CoverageBatchResult>(
            "/coverage_batches",
            Guard.Input(input, nameof(input)),
            Guard.IdempotencyKey(idempotencyKey),
            new Dictionary<string, string> { ["X-Heyrafiki-Artifact-Reference"] = Guard.ArtifactReference(artifactReference) },
            true,
            cancellationToken);
}

/// <summary>Pre-authorization operations.</summary>
public sealed class PreauthorizationsClient
{
    private readonly HeyrafikiTransport _transport;
    internal PreauthorizationsClient(HeyrafikiTransport transport) => _transport = transport;

    /// <summary>Creates a synthetic pre-authorization.</summary>
    public Task<Preauthorization> CreateAsync(PreauthorizationInput input, string idempotencyKey, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<Preauthorization>("/preauthorizations", Guard.Input(input, nameof(input)), Guard.IdempotencyKey(idempotencyKey), null, true, cancellationToken);

    /// <summary>Retrieves a synthetic pre-authorization.</summary>
    public Task<Preauthorization> RetrieveAsync(string preauthorizationId, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<Preauthorization>($"/preauthorizations/{Guard.Escape(Guard.Identifier(preauthorizationId, "preauth_", nameof(preauthorizationId)))}", cancellationToken);

    /// <summary>Records an accountable pre-authorization decision.</summary>
    public Task<Preauthorization> DecideAsync(
        string preauthorizationId,
        PreauthorizationDecisionInput input,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        _transport.PostAsync<Preauthorization>(
            $"/preauthorizations/{Guard.Escape(Guard.Identifier(preauthorizationId, "preauth_", nameof(preauthorizationId)))}/decisions",
            Guard.Input(input, nameof(input)),
            Guard.IdempotencyKey(idempotencyKey),
            null,
            true,
            cancellationToken);
}

/// <summary>Claim operations.</summary>
public sealed class ClaimsClient
{
    private readonly HeyrafikiTransport _transport;
    internal ClaimsClient(HeyrafikiTransport transport) => _transport = transport;

    /// <summary>Lists synthetic Claim status without Person, payer or clinical identifiers.</summary>
    public Task<ClaimList> ListAsync(int limit = 20, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<ClaimList>($"/claims?limit={Guard.Limit(limit)}", cancellationToken);

    /// <summary>Retrieves a synthetic Claim.</summary>
    public Task<Claim> RetrieveAsync(string claimId, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<Claim>($"/claims/{Guard.Escape(Guard.Identifier(claimId, "clm_", nameof(claimId)))}", cancellationToken);

    /// <summary>Reproduces a Claim valuation from the state known at an inclusive business-time and knowledge-time cutoff.</summary>
    public Task<ClaimValuation> RetrieveValuationAsync(
        string claimId,
        DateTimeOffset valuationAt,
        CancellationToken cancellationToken = default) =>
        _transport.GetAsync<ClaimValuation>(
            $"/claims/{Guard.Escape(Guard.Identifier(claimId, "clm_", nameof(claimId)))}/valuation"
                + $"?valuation_at={Guard.Escape(Guard.Timestamp(valuationAt, nameof(valuationAt)))}",
            cancellationToken);

    /// <summary>Submits a Claim from delivered Care and Benefit evidence.</summary>
    public Task<Claim> CreateAsync(ClaimInput input, string idempotencyKey, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<Claim>("/claims", Guard.Input(input, nameof(input)), Guard.IdempotencyKey(idempotencyKey), null, true, cancellationToken);

    /// <summary>Requests additional evidence for a Claim.</summary>
    public Task<Claim> RequestInformationAsync(
        string claimId,
        ClaimInformationRequestInput input,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        _transport.PostAsync<Claim>(
            $"/claims/{Guard.Escape(Guard.Identifier(claimId, "clm_", nameof(claimId)))}/information_requests",
            Guard.Input(input, nameof(input)),
            Guard.IdempotencyKey(idempotencyKey),
            null,
            true,
            cancellationToken);

    /// <summary>Submits requested evidence for a Claim.</summary>
    public Task<Claim> SubmitEvidenceAsync(
        string claimId,
        ClaimEvidenceInput input,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        _transport.PostAsync<Claim>(
            $"/claims/{Guard.Escape(Guard.Identifier(claimId, "clm_", nameof(claimId)))}/evidence",
            Guard.Input(input, nameof(input)),
            Guard.IdempotencyKey(idempotencyKey),
            null,
            true,
            cancellationToken);

    /// <summary>Records a Claim adjudication.</summary>
    public Task<Claim> AdjudicateAsync(
        string claimId,
        ClaimAdjudicationInput input,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        _transport.PostAsync<Claim>(
            $"/claims/{Guard.Escape(Guard.Identifier(claimId, "clm_", nameof(claimId)))}/adjudications",
            Guard.Input(input, nameof(input)),
            Guard.IdempotencyKey(idempotencyKey),
            null,
            true,
            cancellationToken);
}

/// <summary>Remittance operations.</summary>
public sealed class RemittancesClient
{
    private readonly HeyrafikiTransport _transport;
    internal RemittancesClient(HeyrafikiTransport transport) => _transport = transport;

    /// <summary>Lists synthetic remittance advice.</summary>
    public Task<RemittanceList> ListAsync(int limit = 20, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<RemittanceList>($"/remittances?limit={Guard.Limit(limit)}", cancellationToken);

    /// <summary>Retrieves synthetic remittance advice.</summary>
    public Task<Remittance> RetrieveAsync(string remittanceId, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<Remittance>($"/remittances/{Guard.Escape(Guard.Identifier(remittanceId, "rem_", nameof(remittanceId)))}", cancellationToken);

    /// <summary>Records remittance advice and its Claim allocations.</summary>
    public Task<Remittance> CreateAsync(RemittanceInput input, string idempotencyKey, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<Remittance>("/remittances", Guard.Input(input, nameof(input)), Guard.IdempotencyKey(idempotencyKey), null, true, cancellationToken);
}

/// <summary>Webhook endpoint operations.</summary>
public sealed class WebhookEndpointsClient
{
    private readonly HeyrafikiTransport _transport;
    internal WebhookEndpointsClient(HeyrafikiTransport transport) => _transport = transport;

    /// <summary>Lists tenant-scoped Webhook endpoints.</summary>
    public Task<WebhookEndpointList> ListAsync(int limit = 20, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<WebhookEndpointList>($"/webhook_endpoints?limit={Guard.Limit(limit)}", cancellationToken);

    /// <summary>Retrieves a Webhook endpoint.</summary>
    public Task<WebhookEndpoint> RetrieveAsync(string webhookEndpointId, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<WebhookEndpoint>($"/webhook_endpoints/{Guard.Escape(Guard.Identifier(webhookEndpointId, "whe_", nameof(webhookEndpointId)))}", cancellationToken);

    /// <summary>Creates a Webhook endpoint and returns its signing secret once.</summary>
    public Task<WebhookEndpointWithSecret> CreateAsync(WebhookEndpointInput input, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<WebhookEndpointWithSecret>("/webhook_endpoints", Guard.Input(input, nameof(input)), null, null, false, cancellationToken);

    /// <summary>Disables a Webhook endpoint.</summary>
    public Task<WebhookEndpoint> DisableAsync(string webhookEndpointId, CancellationToken cancellationToken = default) =>
        _transport.DeleteAsync<WebhookEndpoint>($"/webhook_endpoints/{Guard.Escape(Guard.Identifier(webhookEndpointId, "whe_", nameof(webhookEndpointId)))}", cancellationToken);

    /// <summary>Sends a synthetic test event to a Webhook endpoint.</summary>
    public Task<WebhookDelivery> SendTestAsync(string webhookEndpointId, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<WebhookDelivery>(
            $"/webhook_endpoints/{Guard.Escape(Guard.Identifier(webhookEndpointId, "whe_", nameof(webhookEndpointId)))}/test",
            null,
            null,
            null,
            false,
            cancellationToken);
}
