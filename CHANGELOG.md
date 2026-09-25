# Changelog

All notable changes to **KUKULCAN.SharedKernel.Auth** are documented in this file.

The project follows the principles of [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

#### Local Authentication

- Local authentication request and result contracts.
- Password hashing and verification using ASP.NET Core Identity password hashing.
- Local user store backed by `KUKULCAN.SharedKernel.Database`.
- User and tenant-membership persistence.
- Multi-tenant authentication results containing all memberships.
- Tenant-aware integration tests for SQL Server, PostgreSQL and MySQL.
- Tests for unique email persistence constraints.
- Tests for foreign-key integrity and cascading user deletion.
- Tests distinguishing normal tenant-filtered persistence queries from authentication lookups that must retrieve all memberships.
- End-to-end local authentication integration tests for SQL Server, PostgreSQL and MySQL.

#### Federated Authentication

- Federated authentication request and identity contracts.
- Federated authentication service.
- Google, Microsoft and Apple provider wrappers.
- Federated identity persistence using `(Provider, Subject)`.
- Google and Apple stable subject mapping through `sub`.
- Microsoft stable subject mapping through `tid:oid`.
- Federated persistence integration tests for SQL Server, PostgreSQL and MySQL.
- Federated authentication integration specifications covering Google, Microsoft and Apple across all three database providers.
- Provider-specific validator test specifications for Google, Microsoft and Apple.
- Controlled JWT/JWKS test scenarios designed to avoid live provider calls.

#### Testing

- NUnit unit-test project.
- Independent SQL Server, PostgreSQL and MySQL integration-test projects.
- Unit tests for local authentication, password hashing and federated authentication.
- Integration tests for persistence, tenant behavior, cancellation and federated identity constraints.
- Coverage-audit documentation.

### Changed

- Authentication persistence is explicitly built on `KUKULCAN.SharedKernel.Database`.
- Authentication reuses `KUKULCAN.SharedKernel` result and error contracts.
- Federated identity handling is based on stable provider identifiers rather than email addresses.
- Authentication preserves complete tenant membership information.

### Security

- Password hashes are kept inside persistence models and are not exposed in authenticated-user results.
- Federated identity contracts require provider-specific stable identifiers.
- Provider validator tests specify rejection of invalid issuer, audience, expiry, required claims and signatures.

## Release Notes

No stable public release version is declared in this changelog yet. Versioned release entries will be added when releases are published.
