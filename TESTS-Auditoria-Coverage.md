# Tests — Coverage Audit

## Purpose

This document defines how **KUKULCAN.SharedKernel.Auth** audits test quality, behavior coverage and execution coverage.

The objective is **not** to maximize a coverage percentage artificially. Coverage is an audit signal combined with the functional matrix.

## Test Architecture

### Unit Tests

`Tests/KUKULCAN.SharedKernel.Auth.UnitTests` validates deterministic behavior without a real database or live provider.

Coverage includes local authentication, email normalization, password verification, invalid credentials, tenant membership behavior, password hashing, federated provider selection, provider-wrapper behavior, provider mismatch and identity-not-linked behavior, cancellation, Google/Microsoft/Apple credential validation, OIDC/JWKS failures, malformed Base64Url and token claim/signature validation.

### Integration Tests

Authentication persistence is validated independently against SQL Server, PostgreSQL and MySQL using the real `KUKULCAN.SharedKernel.Database` infrastructure.

The suites validate user persistence, tenant memberships, federated identities, unique constraints, foreign keys, cascade deletion, tenant query filters, complete membership retrieval, active tenant access, active tenant switching, email canonicalization, sync/async persistence, cancellation and local/federated end-to-end behavior.

## Current Test Matrix

| Test project | Executed | Passed | Status |
|---|---:|---:|---|
| UnitTests | 129 | 129 | GREEN |
| PostgreSQL Integration | 41 | 41 | GREEN |
| SQL Server Integration | 41 | 41 | GREEN |
| MySQL Integration | 41 | 41 | GREEN |
| **Total** | **252** | **252** | **GREEN** |

No functional test gap is currently justified solely by the existing authentication contract.

## Multi-Tenant Audit

The tests verify that:

1. successful authentication returns all tenant memberships;
2. `Auth.NoTenantAccess` is returned when there is no active-tenant membership;
3. users cannot inherit another user's memberships;
4. normal persistence queries respect the active tenant filter;
5. authentication stores can bypass the filter when reconstructing complete memberships;
6. active tenant switching does not leave stale state;
7. membership changes persist correctly.

## AuthDbContext Audit

`AuthDbContext` overrides four relevant methods:

1. `SaveChanges(bool acceptAllChangesOnSuccess)`;
2. `SaveChanges()`;
3. `SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken)`;
4. `SaveChangesAsync(CancellationToken)`.

Custom logic processes Added and Modified `AuthUserEntity` entries:

```
Email = Email.Trim().ToLowerInvariant()
```

The integration suites cover added-user canonicalization through `SaveChanges()`, added-user canonicalization through `SaveChangesAsync()`, modified-user canonicalization and real database round-trips.

The three providers execute this contract independently.

`SaveChanges(false)` and cancellation are not additional authentication behaviors: the boolean overload delegates to EF Core after the same custom canonicalization, and cancellation is passed through to EF Core/provider execution. No artificial tests are required for those inherited semantics.

## Federated Provider Audit

Google, Microsoft and Apple validators cover valid credentials, required claims, issuer, audience, lifetime, signatures, supported algorithms, OIDC configuration, JWKS discovery, empty/unusable signing keys, HTTP metadata failures, malformed Base64Url and cancellation.

| Provider | Stable subject |
|---|---|
| Google | `sub` |
| Microsoft | `tid:oid` |
| Apple | `sub` |

## Deterministic Provider Testing

Validator tests use generated RSA keys, controlled JWT claims, deterministic OIDC metadata, deterministic JWKS payloads, custom HTTP handlers, controlled HTTP failures and controlled cancellation.

They do not call live identity-provider services.

## TDD Relationship

The repository follows:

```
TEST → RED → Source → GREEN → Coverage → PR → merge
```

A RED test is meaningful only when it specifies behavior missing from production code. If production code already satisfies a newly formalized behavior, the test may be immediately GREEN. Production code must not be changed to manufacture RED.

## Coverage Method

`.github/workflows/coverage.yml` uses Cobertura and restores/builds the solution, runs UnitTests plus PostgreSQL, SQL Server and MySQL integration tests with coverage, and publishes one Cobertura artifact per test project.

Production coverage includes only `KUKULCAN.SharedKernel.Auth` and excludes all test assemblies and compiler-generated code.

## Coverage Interpretation

Coverage must be interpreted by behavior, not percentage alone. Priority is given to branches controlling credential acceptance, signature validation, issuer/audience validation, provider identity mapping, tenant membership exposure, active tenant access and persistence integrity.

## What Must Not Be Done

Do not add tests solely to increase coverage. Avoid duplicate tests without additional contract, trivial accessor-only tests, fabricated invalid states solely for defensive branches, mocks replacing real persistence in integration tests, live external-provider calls and arbitrary global coverage thresholds.

## Project File Audit

The four test projects rely on implicit SDK compilation.

No `<Compile Remove>` or `<Compile Update>` entries are present in:

- `KUKULCAN.SharedKernel.Auth.UnitTests.csproj`;
- `KUKULCAN.SharedKernel.Auth.SQLServer.Integration.csproj`;
- `KUKULCAN.SharedKernel.Auth.PostgreSQL.Integration.csproj`;
- `KUKULCAN.SharedKernel.Auth.MySQL.Integration.csproj`.

The former API test exclusion was removed together with the obsolete API test because this repository is a class library.

There are currently no `.cs` test files intentionally excluded from compilation.

## Coverage Conclusion

The current audit concludes that the authentication behavior is fully covered according to the defined contract.

Future tests should be introduced only when a new authentication behavior, security requirement, persistence rule or regression requires them.
