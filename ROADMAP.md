# Roadmap

## Purpose

This roadmap describes the intended evolution of **KUKULCAN.SharedKernel.Auth**. It communicates direction rather than fixed delivery dates.

Architectural correctness, security, testability and compatibility take precedence over feature volume.

## Current Status

The authentication foundation is implemented and behavior-tested.

Current scope includes local authentication, password hashing, multi-tenant authentication, active-tenant boundaries, complete membership reconstruction, Google/Microsoft/Apple federated authentication, OIDC/JWKS/JWT validation, federated identity persistence, SQL Server/PostgreSQL/MySQL integration, deterministic NUnit tests, TDD and coverage auditing.

The current `main` suite is GREEN: **252 / 252**.

## Completed

### Local Authentication

- request validation;
- email normalization;
- password verification;
- invalid credentials;
- user and membership persistence;
- active tenant boundary;
- active tenant switching;
- complete membership preservation;
- user isolation;
- cancellation.

### Persistence

- `AuthDbContext` integration with `KUKULCAN.SharedKernel.Database`;
- email canonicalization for added and modified users;
- synchronous and asynchronous save paths;
- foreign-key integrity;
- federated identity uniqueness;
- cascade deletion;
- tenant query filters;
- real SQL Server, PostgreSQL and MySQL round-trips.

### Federated Authentication

- provider resolution;
- Google, Microsoft and Apple provider contracts;
- stable subject mapping;
- federated identity lookup;
- provider mismatch and identity-not-linked handling;
- active tenant boundary;
- all-membership preservation;
- cancellation propagation.

### Credential Validators

Google, Microsoft and Apple validators cover valid credentials, required claims, issuer, audience, lifetime, signatures, supported algorithms, OIDC configuration, JWKS discovery, empty/unusable signing keys, HTTP metadata failures, malformed Base64Url and cancellation.

Invalid credential infrastructure failures are normalized to `Auth.FederatedCredentialInvalid`.

## Test and Coverage Status

The functional audit found no remaining test justified solely by the current authentication contract.

| Project                |        Status |
|------------------------|--------------:|
| UnitTests              |     129 / 129 |
| PostgreSQL Integration |       41 / 41 |
| SQL Server Integration |       41 / 41 |
| MySQL Integration      |       41 / 41 |
| **Total**              | **252 / 252** |

The project does not add artificial tests to pursue a percentage target.

## Next Development Areas

### Application/API Host Integration

`KUKULCAN.SharedKernel.Auth` remains a class library. When an application requires HTTP authentication, the application or a separate host should own tests for endpoints, DTOs, HTTP status/error mapping, JWT issuance, tenant-context resolution and transport security.

The API host must not be introduced into `Source/KUKULCAN.SharedKernel.Auth`.

### Production Provider Configuration

Future operational documentation may standardize Google, Microsoft and Apple configuration, secure credential storage, provider metadata/key rotation and diagnostics that do not expose credentials or tokens.

### Packaging and Release

When a formal public release is prepared, document package metadata, release/versioning policy, SourceLink/repository metadata, NuGet publication and compatibility policy.

## Architectural Boundaries

The project must not become a general authorization framework, application-specific user-management system, generic repository framework, replacement for `KUKULCAN.SharedKernel.Database`, general-purpose identity provider or ASP.NET Core application host.

New functionality should enter the shared component only when it is a genuine cross-application authentication requirement.

## Quality Goals

Maintain deterministic unit tests, real provider-backed integration tests, tenant-isolation regression tests, security regression tests, explicit cancellation behavior, minimal public API, XML documentation and behavior-based coverage auditing.

Future behavior changes continue to follow:

```
TEST → RED → Source → GREEN → Coverage → PR → merge
```
