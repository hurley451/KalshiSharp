# KalshiCore Conformance

KalshiSharp is the first reference SDK target for
[KalshiCore](https://github.com/hurley451/KalshiCore), the shared contract,
fixture, and conformance framework for Kalshi SDK implementations.

KalshiCore owns reviewed wire-level expectations. KalshiSharp owns the
idiomatic .NET client API and validates that API against KalshiCore fixtures
through `KalshiSharp.Conformance`.

## Local Check

From a KalshiSharp checkout:

```bash
dotnet restore KalshiSharp.sln
dotnet build KalshiSharp.sln --configuration Release --no-restore -p:EnablePackageValidation=false
dotnet run --project KalshiSharp.Conformance/KalshiSharp.Conformance.csproj --configuration Release --no-build -- --kalshicore-root /path/to/KalshiCore --report artifacts/kalshicore-conformance/report.json
```

The adapter currently runs deterministic offline fixtures only. Live Kalshi API
checks should remain separate and opt-in.

## Scheduled Drift Reporting

`.github/workflows/kalshicore-conformance.yml` runs on pull requests, pushes to
`main`, manual dispatch, and a daily schedule. Scheduled failures upload the
conformance report and, when the `KALSHICORE_ISSUE_TOKEN` repository secret is
configured with issue access to `hurley451/KalshiCore`, create or update a
`contract-gap` issue in the parent repository.

## Reporting Gaps

If KalshiSharp reveals a missing or incorrect shared contract, file a
`Contract gap` issue in KalshiCore with:

- The affected endpoint or WebSocket behavior.
- The failing fixture or adapter check.
- The KalshiSharp impact.
- Official source links or observed evidence.
- The expected parent contract or fixture change.
