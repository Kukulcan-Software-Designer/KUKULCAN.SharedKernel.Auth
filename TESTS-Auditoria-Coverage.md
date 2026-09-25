# Tests — Coverage Audit

## Purpose

This document describes how **KUKULCAN.SharedKernel.Auth** audits the quality and coverage of its automated tests.

The objective is not to maximize a coverage percentage artificially. Coverage is used as an audit signal to identify executable code paths that are not represented by meaningful tests.

## Test Architecture

The repository separates tests by responsibility and database provider.

### Unit Tests

`Tests/KUKULCAN.SharedKernel.Auth.UnitTests`

Unit tests validate deterministic application and authentication behavior without requiring a real database or external identity provider.

They cover:

- local authentication request validation;
- email normalization;
- password verification;
- invalid credentials;
- tenant membership handling;
- password hashing;
- federated provider selection;
- provider-wrapper behavior;
- federated identity lookup rules;
- provider mismatch handling;
- cancellation and exception propagation;
- provider-specific token-validation contracts.

## Integration Tests

Authentication persistence is validated independently against:

- SQL Server;
- PostgreSQL;
- MySQL.

The integration suites use the real **KUKULCAN.SharedKernel.Database** infrastructure rather than replacing persistence with a parallel test abstraction.

They audit:

- user persistence;
- tenant membership persistence;
- federated identity persistence;
- unique constraints;
- foreign keys;
- cascade deletion;
- tenant query filters;
- retrieval of all tenant memberships by the authentication stores;
- cancellation behavior;
- local authentication end-to-end behavior.

## Federated Authentication Audit

Google, Microsoft and Apple are tested independently.

The test contract validates:

- trusted JWT signatures;
- issuer validation;
- audience validation;
- expiration validation;
- required subject claims;
- Google subject mapping through `sub`;
- Apple subject mapping through `sub`;
- Microsoft subject mapping through `tid:oid`.

Provider tests use controlled cryptographic material and deterministic HTTP responses for JWKS/OIDC metadata. The purpose is to test validation behavior without depending on live Google, Microsoft or Apple services.

## Multi-Tenant Audit

Multi-tenancy is audited at both unit and persistence levels.

The tests verify that:

1. authentication returns all tenant memberships belonging to the authenticated user;
2. users cannot inherit another user's memberships;
3. normal persistence queries respect the active tenant filter;
4. authentication stores can deliberately bypass the active tenant query filter when they must reconstruct the complete membership set;
5. tenant membership changes are persisted correctly.

This distinction is important because the active `ITenantContext` represents the tenant context of persistence operations, while authentication must be able to establish the complete set of memberships for a user.

## Coverage Audit Method

Coverage should be generated from the complete solution test run using the repository's .NET test and coverage tooling.

The audit should consider at least:

- line coverage;
- branch coverage;
- method coverage;
- uncovered branches in security-sensitive code;
- uncovered exception paths;
- provider-specific behavior;
- database-provider-specific behavior.

A high line-coverage percentage is not sufficient when important branches remain untested.

## What Must Not Be Done

Do not add artificial tests solely to increase coverage.

Examples of tests that should be avoided:

- tests that only execute a property getter without behavioral value;
- duplicated tests with no additional contract;
- tests whose only purpose is to satisfy a coverage threshold;
- tests that mock away the persistence behavior they are intended to verify;
- live external-provider calls that make the test suite non-deterministic.

## TDD Relationship

The authentication project follows a test-first development model.

A test may intentionally be RED when it specifies behavior that has not yet been implemented. Such a test is part of the executable contract and should not be removed merely because it temporarily lowers runtime coverage.

The expected lifecycle is:

```text
Test specification
      ↓
RED
      ↓
Minimal implementation
      ↓
GREEN
      ↓
Refactor
      ↓
Coverage audit
```

## Provider Matrix

| Area | Unit | SQL Server | PostgreSQL | MySQL |
|---|---:|---:|---:|---:|
| Local authentication | ✓ | ✓ | ✓ | ✓ |
| Google persistence/integration specification | ✓ | ✓ | ✓ | ✓ |
| Microsoft persistence/integration specification | ✓ | ✓ | ✓ | ✓ |
| Apple persistence/integration specification | ✓ | ✓ | ✓ | ✓ |
| Provider validator specification | ✓ | ✓* | ✓* | ✓* |
| Multi-tenant behavior | ✓ | ✓ | ✓ | ✓ |

* The database-side federated end-to-end tests are present as specifications and depend on the provider credential validators being implemented.

## Interpreting Results

Coverage reports should always be interpreted together with the test matrix.

A missing line in a trivial accessor is less significant than an uncovered branch that controls:

- credential acceptance;
- token signature validation;
- issuer or audience validation;
- tenant membership exposure;
- provider identity mapping;
- authorization-relevant persistence behavior.

The audit therefore combines quantitative coverage with qualitative review of security-sensitive and tenant-sensitive paths.

## Reporting

Coverage reports should be generated from the same source revision used for the test run and retained with the corresponding development or release evidence.

When coverage changes materially, this document should be updated if the test architecture or audit methodology changes.

The audit must distinguish between:

- tests that are GREEN and validate implemented behavior;
- test-first specifications that are intentionally RED;
- provider-specific integration tests;
- persistence behavior executed against real database engines.

This prevents a coverage report from implying that unimplemented authentication behavior has already been validated.
