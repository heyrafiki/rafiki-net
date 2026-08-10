# Release process

## Release gates

1. `heyrafiki/rafiki-net` has a named release owner and protected `main` branch.
2. The pinned OpenAPI revision is on `main`, its SHA-256 digest matches the lock,
   and `info.license` declares `Apache 2.0` with identifier `Apache-2.0`.
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

Inspect the `.nupkg` and `.snupkg`, then create a signed GitHub release tagged
`sdk-v<version>`. Publish only after every release gate above is verified.
