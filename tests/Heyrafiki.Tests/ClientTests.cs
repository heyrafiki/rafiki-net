using System.Globalization;
using System.Net;
using System.Text;
using Heyrafiki.Models;
using Xunit;

namespace Heyrafiki.Tests;

public sealed class ClientTests
{
    [Fact]
    public async Task SendsBearerAuthenticationAndParsesAList()
    {
        var handler = new RecordingHandler(Response(HttpStatusCode.OK, """
            {"object":"list","data":[],"has_more":false}
            """));
        using var httpClient = new HttpClient(handler);
        using var client = CreateClient(httpClient);

        var result = await client.Practitioners.ListAsync(10);

        Assert.False(result.HasMore);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://api.heyrafiki.space/v1/practitioners?limit=10", request.Uri.ToString());
        Assert.Equal("Bearer", request.AuthorizationScheme);
        Assert.Equal("test_api_key", request.AuthorizationParameter);
        Assert.Equal("heyrafiki-dotnet/0.1.0-beta.1", request.UserAgent);
    }

    [Fact]
    public async Task SupportsTheApiKeyHeaderScheme()
    {
        var handler = new RecordingHandler(Response(HttpStatusCode.OK, """
            {"object":"api","version":"v1","environment":"sandbox","resources":[]}
            """));
        using var httpClient = new HttpClient(handler);
        using var client = new HeyrafikiClient(new HeyrafikiClientOptions
        {
            ApiKey = "test_api_key",
            AuthenticationScheme = HeyrafikiAuthenticationScheme.ApiKeyHeader,
        }, httpClient);

        await client.Api.RetrieveAsync();

        var request = Assert.Single(handler.Requests);
        Assert.Null(request.AuthorizationScheme);
        Assert.Equal("test_api_key", request.ApiKey);
    }

    [Fact]
    public async Task SendsCallerOwnedIdempotencyKeyWithoutChangingTheBody()
    {
        var handler = new RecordingHandler(Response(HttpStatusCode.Created, BookingJson));
        using var httpClient = new HttpClient(handler);
        using var client = CreateClient(httpClient);
        var input = new BookingInput
        {
            PractitionerId = "prc_2481",
            StartsAt = DateTimeOffset.Parse("2026-08-12T07:00:00Z", CultureInfo.InvariantCulture),
            EndsAt = DateTimeOffset.Parse("2026-08-12T08:00:00Z", CultureInfo.InvariantCulture),
            Format = "online",
            PaymentSource = "covered",
        };

        await client.Bookings.CreateAsync(input, "booking-demo-001");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("booking-demo-001", request.IdempotencyKey);
        Assert.Contains("\"practitioner_id\":\"prc_2481\"", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreservesOmittedAndExplicitNullRequestProperties()
    {
        var handler = new RecordingHandler(
            Response(HttpStatusCode.Created, ClaimJson),
            Response(HttpStatusCode.Created, ClaimJson));
        using var httpClient = new HttpClient(handler);
        using var client = CreateClient(httpClient);
        var omitted = new ClaimInformationRequestInput
        {
            ReasonCode = "clinical_note",
            RequestedEvidenceTypes = ["clinical_note"],
        };
        var explicitNull = new ClaimInformationRequestInput
        {
            ReasonCode = "clinical_note",
            RequestedEvidenceTypes = ["clinical_note"],
            DueAt = new RequestValue<DateTimeOffset?>(null),
        };

        await client.Claims.RequestInformationAsync("clm_demo_001", omitted, "request-demo-001");
        await client.Claims.RequestInformationAsync("clm_demo_001", explicitNull, "request-demo-002");

        Assert.DoesNotContain("due_at", handler.Requests[0].Body, StringComparison.Ordinal);
        Assert.Contains("\"due_at\":null", handler.Requests[1].Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RetriesReadsOnServiceUnavailable()
    {
        var handler = new RecordingHandler(
            Response(HttpStatusCode.ServiceUnavailable, ErrorJson),
            Response(HttpStatusCode.OK, "{\"object\":\"list\",\"data\":[],\"has_more\":false}"));
        using var httpClient = new HttpClient(handler);
        using var client = CreateClient(httpClient, maxRetries: 1);

        await client.Sessions.ListAsync();

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task RetriesIdempotentWritesWithTheSameKeyAndBody()
    {
        var handler = new RecordingHandler(
            Response((HttpStatusCode)429, ErrorJson),
            Response(HttpStatusCode.Created, BookingJson));
        using var httpClient = new HttpClient(handler);
        using var client = CreateClient(httpClient, maxRetries: 1);
        var input = new BookingInput
        {
            PractitionerId = "prc_2481",
            StartsAt = DateTimeOffset.Parse("2026-08-12T07:00:00Z", CultureInfo.InvariantCulture),
            EndsAt = DateTimeOffset.Parse("2026-08-12T08:00:00Z", CultureInfo.InvariantCulture),
            Format = "online",
            PaymentSource = "covered",
        };

        await client.Bookings.CreateAsync(input, "booking-demo-001");

        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request => Assert.Equal("booking-demo-001", request.IdempotencyKey));
        Assert.Equal(handler.Requests[0].Body, handler.Requests[1].Body);
    }

    [Fact]
    public async Task DoesNotRetryWebhookWrites()
    {
        var handler = new RecordingHandler(Response(HttpStatusCode.ServiceUnavailable, ErrorJson));
        using var httpClient = new HttpClient(handler);
        using var client = CreateClient(httpClient, maxRetries: 3);

        await Assert.ThrowsAsync<HeyrafikiApiException>(() => client.WebhookEndpoints.CreateAsync(new WebhookEndpointInput
        {
            Url = new Uri("https://hooks.example.com/heyrafiki"),
            Events = ["sandbox.ping"],
        }));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task ExposesTheStableErrorEnvelopeWithoutRawContent()
    {
        var response = Response(HttpStatusCode.Forbidden, """
            {"error":{"code":"permission_denied","message":"This key cannot access the resource.","docs":"https://docs.heyrafiki.space/errors"}}
            """);
        response.Headers.Add("X-Request-Id", "req_123");
        var handler = new RecordingHandler(response);
        using var httpClient = new HttpClient(handler);
        using var client = CreateClient(httpClient);

        var error = await Assert.ThrowsAsync<HeyrafikiApiException>(() =>
            client.Practitioners.RetrieveAsync("prc_2481"));

        Assert.Equal(HttpStatusCode.Forbidden, error.StatusCode);
        Assert.Equal("permission_denied", error.Code);
        Assert.Equal("req_123", error.RequestId);
        Assert.Equal("https://docs.heyrafiki.space/errors", error.Docs);
        Assert.DoesNotContain("error", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendsTheCoverageBatchArtifactReference()
    {
        var handler = new RecordingHandler(Response(HttpStatusCode.Created, """
            {"object":"coverage_batch_result","batch_reference":"batch:001","artifact_sha256":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","total":1,"recorded":1,"replayed":0,"observations":[]}
            """));
        using var httpClient = new HttpClient(handler);
        using var client = CreateClient(httpClient);

        await client.CoverageBatches.RecordAsync(new CoverageBatchInput
        {
            ContractVersion = "2026-08-01",
            BatchReference = "batch:001",
            BatchVersion = "1",
            SourceContractReference = "payer:contract:001",
            GeneratedAt = DateTimeOffset.Parse("2026-08-12T07:00:00Z", CultureInfo.InvariantCulture),
            Records = [],
        }, "artifact:coverage:001", "coverage-batch-001");

        Assert.Equal("artifact:coverage:001", Assert.Single(handler.Requests).ArtifactReference);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task RejectsLimitsOutsideTheContract(int limit)
    {
        using var client = CreateClient(new HttpClient(new RecordingHandler()));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.Claims.ListAsync(limit));
    }

    [Fact]
    public async Task ImplementsEveryPublishedOperationWithoutAdditionalRoutes()
    {
        var handler = new RecordingHandler(Enumerable.Range(0, 30)
            .Select(_ => Response(HttpStatusCode.OK, "{}"))
            .ToArray());
        using var httpClient = new HttpClient(handler);
        using var client = CreateClient(httpClient);
        const string key = "contract-test-key";

        await client.Api.RetrieveAsync();
        await client.Practitioners.ListAsync();
        await client.Practitioners.RetrieveAsync("prc_1");
        await client.Practitioners.RetrieveAvailabilityAsync("prc_1");
        await client.Bookings.ListAsync();
        await client.Bookings.CreateAsync(new BookingInput(), key);
        await client.Bookings.RetrieveAsync("bkg_1");
        await client.Sessions.ListAsync();
        await client.Sessions.RetrieveAsync("ses_1");
        await client.EligibilityChecks.CreateAsync(new EligibilityCheckInput(), key);
        await client.EligibilityChecks.RetrieveAsync("elig_1");
        await client.Coverages.RecordAsync(new CoverageObservationInput(), key);
        await client.CoverageBatches.RecordAsync(new CoverageBatchInput(), "artifact:1", key);
        await client.Preauthorizations.CreateAsync(new PreauthorizationInput(), key);
        await client.Preauthorizations.RetrieveAsync("preauth_1");
        await client.Preauthorizations.DecideAsync("preauth_1", new DenyPreauthorizationInput(), key);
        await client.Claims.ListAsync();
        await client.Claims.CreateAsync(new ClaimInput(), key);
        await client.Claims.RetrieveAsync("clm_1");
        await client.Claims.RequestInformationAsync("clm_1", new ClaimInformationRequestInput(), key);
        await client.Claims.SubmitEvidenceAsync("clm_1", new ClaimEvidenceInput(), key);
        await client.Claims.AdjudicateAsync("clm_1", new ClaimAdjudicationInput(), key);
        await client.Remittances.ListAsync();
        await client.Remittances.CreateAsync(new RemittanceInput(), key);
        await client.Remittances.RetrieveAsync("rem_1");
        await client.WebhookEndpoints.ListAsync();
        await client.WebhookEndpoints.CreateAsync(new WebhookEndpointInput());
        await client.WebhookEndpoints.RetrieveAsync("whe_1");
        await client.WebhookEndpoints.DisableAsync("whe_1");
        await client.WebhookEndpoints.SendTestAsync("whe_1");

        var operations = handler.Requests.Select(request => $"{request.Method} {request.Uri.PathAndQuery}").ToArray();
        Assert.Equal(
        [
            "GET /v1/",
            "GET /v1/practitioners?limit=20",
            "GET /v1/practitioners/prc_1",
            "GET /v1/practitioners/prc_1/availability",
            "GET /v1/bookings?limit=20",
            "POST /v1/bookings",
            "GET /v1/bookings/bkg_1",
            "GET /v1/sessions?limit=20",
            "GET /v1/sessions/ses_1",
            "POST /v1/eligibility_checks",
            "GET /v1/eligibility_checks/elig_1",
            "POST /v1/coverages",
            "POST /v1/coverage_batches",
            "POST /v1/preauthorizations",
            "GET /v1/preauthorizations/preauth_1",
            "POST /v1/preauthorizations/preauth_1/decisions",
            "GET /v1/claims?limit=20",
            "POST /v1/claims",
            "GET /v1/claims/clm_1",
            "POST /v1/claims/clm_1/information_requests",
            "POST /v1/claims/clm_1/evidence",
            "POST /v1/claims/clm_1/adjudications",
            "GET /v1/remittances?limit=20",
            "POST /v1/remittances",
            "GET /v1/remittances/rem_1",
            "GET /v1/webhook_endpoints?limit=20",
            "POST /v1/webhook_endpoints",
            "GET /v1/webhook_endpoints/whe_1",
            "DELETE /v1/webhook_endpoints/whe_1",
            "POST /v1/webhook_endpoints/whe_1/test",
        ], operations);

        var idempotentWrites = handler.Requests.Where(request => request.IdempotencyKey is not null).ToArray();
        Assert.Equal(11, idempotentWrites.Length);
        Assert.All(idempotentWrites, request => Assert.Equal(key, request.IdempotencyKey));
    }

    private static HeyrafikiClient CreateClient(HttpClient httpClient, int maxRetries = 0) =>
        new(new HeyrafikiClientOptions
        {
            ApiKey = "test_api_key",
            MaxRetries = maxRetries,
            InitialRetryDelay = TimeSpan.Zero,
            MaximumRetryDelay = TimeSpan.Zero,
        }, httpClient);

    private static HttpResponseMessage Response(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json"),
    };

    private const string ErrorJson = "{\"error\":{\"code\":\"service_unavailable\",\"message\":\"Try again.\",\"docs\":\"https://docs.heyrafiki.space/errors\"}}";
    private const string BookingJson = "{\"id\":\"bkg_demo_001\",\"object\":\"booking\",\"session_id\":\"ses_demo_001\",\"practitioner_id\":\"prc_2481\",\"starts_at\":\"2026-08-12T07:00:00Z\",\"ends_at\":\"2026-08-12T08:00:00Z\",\"timezone\":\"Africa/Nairobi\",\"format\":\"online\",\"status\":\"reserved\",\"payment_source\":\"covered\"}";
    private const string ClaimJson = "{\"id\":\"clm_demo_001\",\"object\":\"claim\",\"status\":\"queried\",\"provider_claim_reference\":\"provider:claim:001\",\"submission_version\":1,\"amount\":{\"currency\":\"KES\",\"billed\":350000,\"approved\":null,\"remitted\":null,\"settled\":null},\"service_period\":{\"starts_at\":\"2026-08-12T07:00:00Z\",\"ends_at\":\"2026-08-12T08:00:00Z\"},\"lines\":[],\"information_requests\":[],\"adjudication\":null,\"submitted_at\":\"2026-08-12T08:01:00Z\",\"updated_at\":\"2026-08-12T08:01:00Z\"}";

    private sealed class RecordingHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public List<RequestSnapshot> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RequestSnapshot(
                request.Method,
                request.RequestUri!,
                request.Headers.Authorization?.Scheme,
                request.Headers.Authorization?.Parameter,
                Header(request, "x-api-key"),
                Header(request, "Idempotency-Key"),
                Header(request, "X-Heyrafiki-Artifact-Reference"),
                Header(request, "User-Agent"),
                body));
            return _responses.Count > 0
                ? _responses.Dequeue()
                : throw new InvalidOperationException("No response was configured.");
        }

        private static string? Header(HttpRequestMessage request, string name) =>
            request.Headers.TryGetValues(name, out var values) ? values.SingleOrDefault() : null;
    }

    private sealed record RequestSnapshot(
        HttpMethod Method,
        Uri Uri,
        string? AuthorizationScheme,
        string? AuthorizationParameter,
        string? ApiKey,
        string? IdempotencyKey,
        string? ArtifactReference,
        string? UserAgent,
        string Body);
}
