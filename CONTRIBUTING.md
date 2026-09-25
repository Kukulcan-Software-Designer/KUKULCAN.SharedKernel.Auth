# Contributing to KUKULCAN.SharedKernel.Auth

Thank you for contributing to **KUKULCAN.SharedKernel.Auth**.

The project provides reusable authentication infrastructure for the KUKULCAN ecosystem. Contributions must preserve security, multi-tenant correctness, architectural consistency and a small, maintainable public API.

## Before Contributing

Before opening an Issue or Pull Request:

- Read the README and project documentation.
- Search existing Issues, Discussions and Pull Requests.
- Verify that the behavior is not already provided by `KUKULCAN.SharedKernel` or `KUKULCAN.SharedKernel.Database`.
- Confirm that the proposed change belongs in the authentication boundary.

## Architecture

The project targets **.NET 10** and is organized around:

- `Source/` for production code.
- `Tests/` for automated tests.
- `Documentation/` for project documentation.

Authentication persistence must use **KUKULCAN.SharedKernel.Database**. Do not introduce a parallel persistence infrastructure when an existing shared abstraction is applicable.

The project must reuse contracts from **KUKULCAN.SharedKernel** instead of duplicating existing shared concepts.

## Multi-Tenancy

Multi-tenancy is part of the authentication contract.

Authentication results must preserve **all KUKULCAN tenant memberships** associated with the authenticated user. An implementation must not reduce a user's memberships to the currently active tenant.

The persistence-level tenant context is not a substitute for the complete membership set required during authentication.

## Testing and TDD

Tests are the executable specification for authentication behavior.

The preferred development sequence is:

1. Define the required behavior with tests.
2. Implement the minimum production code required by those tests.
3. Run the relevant test suite.
4. Refactor without changing the contract.
5. Repeat for the next behavior.

Do not add tests merely to increase a coverage percentage. Tests must represent real behavior, persistence rules or security requirements.

### Test projects

The repository contains independent NUnit test projects for:

- Unit tests.
- SQL Server integration.
- PostgreSQL integration.
- MySQL integration.

Provider-backed tests should exercise real database behavior rather than replacing persistence with mocks.

## Federated Authentication

Google, Microsoft and Apple integrations must validate provider credentials securely.

Stable external identities are used for persistence:

- Google: `sub`.
- Microsoft: `tid:oid`.
- Apple: `sub`.

Email addresses must not replace the provider's stable identity identifier.

Provider validation should be deterministic in tests and should not require live calls to external identity providers.

## Public API

Keep the public API minimal.

Before introducing a new public abstraction:

- verify that an existing SharedKernel contract cannot be reused;
- document the architectural reason;
- add appropriate tests;
- consider backward compatibility.

All public APIs should include XML documentation.

## Coding Standards

Follow the repository's existing C# conventions:

- .NET 10.
- Nullable reference types enabled.
- Implicit usings enabled.
- Latest supported C# language version.
- Warnings treated as errors.
- File-scoped namespaces.
- Immutable records and read-only collections where appropriate.

## Security

Authentication code is security-sensitive.

Contributions must consider:

- credential validation;
- token signature, issuer, audience and lifetime validation;
- stable provider identities;
- tenant isolation;
- secret and credential handling;
- information disclosure.

Security vulnerabilities must be reported privately according to [SECURITY.md](SECURITY.md).

## Pull Requests

A Pull Request should explain:

- the problem being solved;
- the architectural impact;
- the tests added or updated;
- the database providers affected;
- security or tenant-isolation implications;
- any compatibility considerations.

A contribution should build without warnings and should leave the relevant test suites passing unless the Pull Request explicitly represents a test-first specification that is intentionally RED.
