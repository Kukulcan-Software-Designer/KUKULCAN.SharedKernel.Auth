# Contributing to KUKULCAN.SharedKernel.Auth

Contributions must preserve authentication correctness, security, multi-tenant isolation and the architectural boundaries of the KUKULCAN shared libraries.

## Before Contributing

- Read the repository documentation.
- Search existing Issues and Pull Requests.
- Verify that the behavior does not already belong to `KUKULCAN.SharedKernel` or `KUKULCAN.SharedKernel.Database`.
- Confirm that the proposed change belongs in the authentication boundary.
- Identify the executable behavior that the change establishes.

## Architecture

The project targets **.NET 10** and uses:

- `Source/` for production code;
- `Tests/` for automated tests;
- repository-level Markdown files for governance and documentation.

The production project is a class library. Do not introduce `Program.cs`, controllers, minimal APIs or an ASP.NET Core host into `Source/KUKULCAN.SharedKernel.Auth`.

Authentication persistence must use `KUKULCAN.SharedKernel.Database`. Do not introduce parallel persistence infrastructure.

## TDD Contract

```
TEST → RED → Source → GREEN → Coverage → PR → merge
```

1. Define the behavior with a test.
2. Confirm RED when the behavior is not implemented.
3. Implement the minimum production change.
4. Confirm GREEN.
5. Run the relevant coverage audit.
6. Open a Pull Request with validation evidence.
7. Merge only after required CI is GREEN.

A test that is already GREEN is valid when it documents an existing contract. Do not change production code merely to manufacture RED.

Do not add tests solely to increase coverage percentages.

## Branches

Feature/behavior branches normally start directly from `Develop`.

Use `FEATURES/<BEHAVIOR>`. Documentation-only work may use `DOCS/<NAME>`.

## Testing

The repository contains:

- `KUKULCAN.SharedKernel.Auth.UnitTests`;
- `KUKULCAN.SharedKernel.Auth.SQLServer.Integration`;
- `KUKULCAN.SharedKernel.Auth.PostgreSQL.Integration`;
- `KUKULCAN.SharedKernel.Auth.MySQL.Integration`.

Unit tests validate deterministic behavior. Integration tests exercise real database behavior and must not mock away the persistence contract being tested.

## Multi-Tenancy

Authentication must enforce the active tenant boundary, preserve all memberships in successful results, prevent cross-user membership exposure and distinguish persistence filtering from authentication membership reconstruction.

Changes affecting tenant behavior require explicit behavior tests.

## Federated Authentication

Supported providers are Google, Microsoft and Apple.

| Provider | Stable subject |
|---|---|
| Google | `sub` |
| Microsoft | `tid:oid` |
| Apple | `sub` |

Provider tests must remain deterministic and must not call live identity-provider services.

## Public API

Keep the public API minimal. Before introducing a public abstraction:

- verify that an existing shared contract cannot be reused;
- document the architectural reason;
- add behavior tests;
- consider compatibility and security;
- add XML documentation.

Public API documentation is enforced by the build.

## Coding Standards

Follow the existing conventions:

- .NET 10;
- nullable reference types;
- implicit usings;
- latest supported C# language version;
- warnings treated as errors;
- XML documentation;
- file-scoped namespaces;
- immutable/read-only models where appropriate;
- explicit cancellation-token propagation.

## Pull Requests

Pull Requests should address one coherent concern, use an English title and description, explain behavior and architectural impact, list tests, identify affected providers, describe security/tenant implications and report validation results.

## Security

Do not commit passwords, tokens, client secrets, signing keys or private keys.

Security vulnerabilities must be reported privately according to [SECURITY.md](SECURITY.md).
