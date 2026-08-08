# Release process

The SDK is a source preview. No NuGet package has been published.

## Release gates

1. The repository plan allocates `heyrafiki/heyrafiki-dotnet` and names a release owner.
2. The public OpenAPI `info.license.name` and `info.license.identifier` fields are
   reconciled with the repository's Apache-2.0 `LICENSE`. They currently state
   `All rights reserved` and `LicenseRef-Heyrafiki-Proprietary`.
3. Generated models match the pinned OpenAPI revision and all 30 operations have focused transport tests.
4. Formatting, build, tests, package validation and package-content inspection pass from a clean checkout.
5. The version and changelog are updated together. Preview versions retain a SemVer prerelease suffix.
6. Branch protection, CODEOWNERS, secret scanning, dependency review and required checks are active.
7. The Heyrafiki NuGet namespace, trusted publishing identity and protected release environment are verified.
8. The package and symbols are provenance-backed. The package is inspected before publication.

## Versioning

The SDK uses Semantic Versioning independently from the API contract. The API
major remains in the base URL. Breaking SDK changes require a package major bump.

Additive response fields, operations, optional parameters, enum values and error
codes are compatible changes. Consumers should preserve unknown string values.

## Candidate

```bash
dotnet restore
dotnet format --verify-no-changes --no-restore
dotnet build -c Release --no-restore
dotnet test -c Release --no-build
dotnet pack src/Heyrafiki/Heyrafiki.csproj -c Release --no-build -o artifacts
```

Inspect the `.nupkg` and `.snupkg`, then create a signed GitHub prerelease tagged
`sdk-v<version>`. NuGet publication stays disabled until every release gate above
is verified.
