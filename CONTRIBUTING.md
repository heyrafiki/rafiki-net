# Contributing

Issues and pull requests are welcome.

## Before opening a change

- Keep operations and models aligned with the published OpenAPI contract.
- Do not add API keys, production payloads, personal information or health data.
- Use synthetic examples.
- Keep public errors free of raw response bodies and credentials.

## Check the change

```bash
dotnet format --verify-no-changes
dotnet build -c Release
dotnet test -c Release
dotnet pack src/Heyrafiki/Heyrafiki.csproj -c Release -o artifacts
```

Open a focused pull request with the behavior, contract revision and checks run.
By contributing, you agree that your contribution is licensed under Apache-2.0.

Follow the [Heyrafiki Code of Conduct](https://github.com/heyrafiki/.github/blob/main/CODE_OF_CONDUCT.md).
