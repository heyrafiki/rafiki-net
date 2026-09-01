# Changelog

## 0.1.0-beta.2 - 2026-08-28

- Added `Claims.RetrieveValuationAsync` for `GET /claims/{claim_id}/valuation`.
- Added the `ClaimValuation`, `ClaimValuationAmount` and `ClaimValuationEvent` models.
- Required an explicit `valuationAt` cutoff and sent it as the caller's own offset.
- Repinned the contract lock to the revision that publishes Claim valuation.

## 0.1.0-beta.1 - 2026-08-10

- Added typed clients for every operation in the Heyrafiki OpenAPI 1.0 contract.
- Added Bearer and `x-api-key` authentication.
- Added structured API errors and request identifiers.
- Added bounded retries for reads and idempotent writes on `429` and `503`.
- Added .NET Standard 2.0, .NET 8 and .NET 10 targets.
