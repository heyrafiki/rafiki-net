# Heyrafiki .NET SDK

Typed .NET client for the Heyrafiki API.

The client supports .NET Standard 2.0, .NET 8 and .NET 10. Keep API keys on the
server.

## Build from source

```bash
git clone https://github.com/heyrafiki/rafiki-net.git
cd rafiki-net
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

Add a project reference from the checked-out source:

```xml
<ProjectReference Include="../rafiki-net/src/Heyrafiki/Heyrafiki.csproj" />
```

## First request

```csharp
using Heyrafiki;

using var heyrafiki = new HeyrafikiClient(new HeyrafikiClientOptions
{
    ApiKey = Environment.GetEnvironmentVariable("HEYRAFIKI_API_KEY")
        ?? throw new InvalidOperationException("HEYRAFIKI_API_KEY is required."),
});

var practitioners = await heyrafiki.Practitioners.ListAsync(limit: 5);
```

Sandbox keys return synthetic data.

## Covered Care

Each retryable Care write requires a caller-owned idempotency key. Reuse the same
key when retrying the same operation.

```csharp
using Heyrafiki.Models;

var scheduledAt = new DateTimeOffset(2026, 8, 12, 7, 0, 0, TimeSpan.Zero);

var eligibility = await heyrafiki.EligibilityChecks.CreateAsync(
    new EligibilityCheckInput
    {
        MemberReference = "member_demo_001",
        ServiceCode = "psychotherapy-60",
        ScheduledAt = scheduledAt,
        Amount = 350000,
        Currency = "KES",
    },
    idempotencyKey: Guid.NewGuid().ToString());

var booking = await heyrafiki.Bookings.CreateAsync(
    new BookingInput
    {
        PractitionerId = "prc_2481",
        StartsAt = scheduledAt,
        EndsAt = scheduledAt.AddHours(1),
        Format = "online",
        PaymentSource = "covered",
    },
    idempotencyKey: Guid.NewGuid().ToString());

await heyrafiki.Preauthorizations.CreateAsync(
    new PreauthorizationInput
    {
        EligibilityCheckId = eligibility.Id,
        BookingId = booking.Id,
    },
    idempotencyKey: Guid.NewGuid().ToString());
```

Amounts use the currency's minor unit. `350000` KES is KES 3,500.00.

## Authentication

Bearer authentication is the default. Clients that cannot set the Authorization
header may use `HeyrafikiAuthenticationScheme.ApiKeyHeader`.

Never send a secret key from a browser, mobile application or public repository.

## Retries

The client retries `429` and `503` responses with bounded exponential backoff and
honours `Retry-After`.

- Read requests are safe to retry.
- Writes with a required idempotency key reuse the same serialized body and key.
- Webhook create, disable and test operations are not retried automatically.
- Other status codes and network failures are returned to the caller immediately.

Set `MaxRetries` to `0` to disable automatic retries.

## Errors

```csharp
try
{
    await heyrafiki.Claims.RetrieveAsync("clm_123");
}
catch (HeyrafikiApiException error)
{
    Console.Error.WriteLine($"{(int)error.StatusCode} {error.Code} {error.RequestId}");
}
```

Branch on `Code`. Use `RequestId` when tracing a failed call. The exception does
not expose raw response content.

## Contract

This client is built from the published
[Heyrafiki OpenAPI 1.0 contract](https://github.com/heyrafiki/contract). The pinned
contract revision and operation list are recorded in `eng/openapi.lock.json`.

The client intentionally has no automatic pagination because the current list
contract does not expose a continuation cursor.

## Develop

```bash
dotnet format --verify-no-changes
dotnet build -c Release
dotnet test -c Release
dotnet pack src/Heyrafiki/Heyrafiki.csproj -c Release -o artifacts
```

## Resources

- [Documentation](https://docs.heyrafiki.space)
- [API contract](https://github.com/heyrafiki/contract)
- [Open insurance assurance benchmark](https://github.com/heyrafiki/proving-ground)
- [Webhooks](https://docs.heyrafiki.space/webhooks)
- [Security](./SECURITY.md)

## License

Licensed under the [Apache License 2.0](./LICENSE).
