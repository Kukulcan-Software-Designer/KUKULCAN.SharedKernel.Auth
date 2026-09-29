# Testing

## Philosophy

Authentication testing is behavior-driven and follows:

```text
TEST -> RED -> Source -> GREEN -> Coverage -> PR -> merge
```

Tests must specify meaningful authentication, security, persistence or regression behavior.

## Test Projects

```text
Tests/
├── KUKULCAN.SharedKernel.Auth.UnitTests/
├── KUKULCAN.SharedKernel.Auth.PostgreSQL.Integration/
├── KUKULCAN.SharedKernel.Auth.SQLServer.Integration/
└── KUKULCAN.SharedKernel.Auth.MySQL.Integration/
```

## Unit Tests

Unit tests validate deterministic behavior without real database servers or live identity providers.

They cover local authentication, password verification, tenant decisions, authenticated-user composition, federated orchestration and provider credential validators.

External OIDC/JWKS interactions are controlled with deterministic HTTP handlers and test metadata.

## Integration Tests

Each relational provider has a dedicated integration suite.

Integration tests validate real:

- persistence;
- unique and foreign-key constraints;
- cascade behavior;
- query filters;
- tenant isolation;
- complete membership retrieval;
- active-tenant access;
- email canonicalization;
- synchronous/asynchronous persistence;
- local and federated authentication.

## Why Three Providers?

Authentication persistence depends on relational behavior. Running the same contract against PostgreSQL, SQL Server and MySQL prevents provider-specific behavior from being mistaken for provider-neutral behavior.

## Multi-Tenant Matrix

| Condition | Expected result |
|---|---|
| No memberships | `Auth.NoTenantAccess` |
| Memberships but no active-tenant access | `Auth.NoTenantAccess` |
| Active-tenant access | Success |
| Successful authentication | All memberships returned |
| Active tenant changes | Membership set remains complete |

## Federated Validation Matrix

Supported providers must validate, as applicable:

- issuer;
- audience;
- lifetime;
- signature;
- supported algorithm;
- stable subject;
- missing required claims;
- unusable signing keys;
- malformed metadata;
- HTTP failures;
- cancellation.

Microsoft additionally validates the tenant/object identity represented by `tid` and `oid`.

## Email Canonicalization

The persistence contract is:

```text
"  USER@Example.COM  "
        |
        v
"user@example.com"
```

Real provider-backed integration tests verify that the canonical value is actually persisted.

## TDD Rules

A production change must be preceded by a corresponding behavior test.

Do not add artificial tests solely for coverage. If production already satisfies a newly formalized behavior, the test may legitimately be GREEN immediately.

## CI and Coverage

The normal CI workflow validates restore, build and tests.

The coverage workflow separately runs unit and provider-backed coverage and publishes Cobertura artifacts.

The coverage workflow should not be interpreted as a replacement for the functional test suites.
