# Changelog

All notable changes to **KUKULCAN.SharedKernel.Auth** are documented here.

The project follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

The current `main` branch contains the completed authentication behavior and its corresponding GREEN test suite.

### Added

#### Local Authentication

- Local authentication request/result contracts.
- Password hashing and verification.
- Local user and tenant-membership persistence.
- Active-tenant access enforcement through `Auth.NoTenantAccess`.
- Preservation of all tenant memberships in successful results.
- Active tenant switching coverage.
- Email canonicalization for persisted users.
- Foreign-key and cascade-delete behavior.

#### Federated Authentication

- Federated authentication contracts and service.
- Google, Microsoft and Apple provider wrappers.
- Federated identity persistence.
- Google and Apple subject mapping through `sub`.
- Microsoft subject mapping through `tid:oid`.
- Provider-specific OIDC/JWKS/JWT credential validation.
- Normalization of invalid credential infrastructure failures to `Auth.FederatedCredentialInvalid`.
- Explicit cancellation propagation.

#### Testing

- NUnit unit tests.
- Independent SQL Server, PostgreSQL and MySQL integration suites.
- Behavior-first TDD coverage.
- Deterministic provider validator tests.
- Real database round-trip persistence tests.
- Multi-tenant isolation and active-tenant boundary tests.
- Coverage-audit documentation.

### Changed

- Authentication persistence uses `KUKULCAN.SharedKernel.Database`.
- Shared results/errors are reused from `KUKULCAN.SharedKernel`.
- Federated identities use stable provider subjects rather than email addresses.
- `AuthDbContext` canonicalizes added and modified emails using `Trim().ToLowerInvariant()`.
- The project is explicitly a .NET 10 class library and does not host an ASP.NET Core API.

### Security

- Password hashes are never returned in authentication results.
- JWT signature, issuer, audience and lifetime validation are enforced.
- Stable provider identifiers are used for federated identity persistence.
- Tenant membership exposure is controlled by the active-tenant boundary.
- Cancellation is not converted into authentication success or generic credential failure.

### Validation Status

| Suite                  |        Result |
|------------------------|--------------:|
| UnitTests              |     129 / 129 |
| PostgreSQL Integration |       41 / 41 |
| SQL Server Integration |       41 / 41 |
| MySQL Integration      |       41 / 41 |
| **Total**              | **252 / 252** |

No artificial tests are maintained solely to increase coverage.

## Release Notes

No stable public release version is declared in this changelog yet.
