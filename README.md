# KUKULCAN.SharedKernel.Auth

> Reusable .NET 10 authentication infrastructure for the KUKULCAN ecosystem, with local authentication, multi-tenancy and federated authentication for Google, Microsoft and Apple.

![.NET](https://img.shields.io/badge/.NET-10-blueviolet)
![Authentication](https://img.shields.io/badge/Authentication-Local%20%2B%20Federated-blue)
![Multi--Tenant](https://img.shields.io/badge/Multi--Tenant-Enabled-success)
![Testing](https://img.shields.io/badge/Testing-NUnit-orange)

## Overview

**KUKULCAN.SharedKernel.Auth** is a reusable .NET 10 **class library** that centralizes authentication behavior shared by KUKULCAN applications.

It provides:

- Local email/password authentication.
- Password hashing and verification.
- Multi-tenant user and membership persistence.
- Federated authentication for Google, Microsoft and Apple.
- OIDC metadata, JWKS and JWT credential validation.
- Stable federated identity persistence.
- Active-tenant access enforcement while preserving all memberships.
- SQL Server, PostgreSQL and MySQL integration tests.
- Behavior-focused NUnit tests developed using TDD.

It is deliberately **not** an ASP.NET Core Web API host.

## Architecture

```text
KUKULCAN.SharedKernel
        ^
        |
KUKULCAN.SharedKernel.Database
        ^
        |
KUKULCAN.SharedKernel.Auth
        ^
        |
Consuming application / API host
```

The library reuses `KUKULCAN.SharedKernel` for shared result/error contracts and `KUKULCAN.SharedKernel.Database` for EF Core persistence and tenant-aware infrastructure. It does not introduce a parallel repository or persistence framework.

## Authentication Model

```text
                         Authentication
                         /             \
                        /               \
               Local Authentication   Federated Authentication
                      |                 |
               LocalUserStore     Provider Validator
                      |           Google / Microsoft / Apple
                      |                 |
                      +--------+--------+
                               |
                       AuthenticatedUser
                       + all memberships
```

A successful authentication result contains the user's complete tenant membership set. The active tenant determines whether authentication is permitted; it does not replace the complete membership collection.

## Local Authentication

The local flow validates the request, canonicalizes the email with `Trim().ToLowerInvariant()`, retrieves the user, verifies the password, evaluates active-tenant access and returns `AuthenticatedUser` with all memberships.

If the user has no memberships or no membership in the active tenant, the service returns `Auth.NoTenantAccess`.

Password hashes remain inside persistence models and are not exposed through authenticated-user results.

## Federated Authentication

Supported providers are Google, Microsoft and Apple.

| Provider  | Stable subject |
|-----------|----------------|
| Google    | `sub`          |
| Microsoft | `tid:oid`      |
| Apple     | `sub`          |

Provider validators verify issuer, audience, lifetime, required claims, signature, supported algorithms and signing keys discovered through JWKS. Invalid provider infrastructure input is normalized to `Auth.FederatedCredentialInvalid`; cancellation propagates as `OperationCanceledException`.

## Multi-Tenancy

```text
No memberships
    -> Auth.NoTenantAccess

Memberships exist, but none for active tenant
    -> Auth.NoTenantAccess

Active-tenant membership exists
    -> Success + all memberships
```

Integration tests verify this contract against real SQL Server, PostgreSQL and MySQL databases, including tenant switching and user isolation.

## Persistence

`AuthDbContext` is built on **KUKULCAN.SharedKernel.Database** and persists users, tenant memberships and federated identities. EF Core migrations are provided in separate PostgreSQL, SQL Server and MySQL migration packages so the main Auth package remains provider-neutral.

For PostgreSQL and SQL Server, the Auth tables are stored in the `Auth` database schema. MySQL keeps the tables in the database selected by the connection string because MySQL treats schemas as databases.

### EF Core Migration Architecture

The migration model deliberately separates the common Auth model from provider-specific migration artifacts:

```text
KUKULCAN.SharedKernel.Auth
        |
        +-- Auth.Migrations.PostgreSQL
        |
        +-- Auth.Migrations.SQLServer
        |
        +-- Auth.Migrations.MySQL
```

Each migration project has its own EF Core provider, design-time factory, generated migrations and model snapshot.

The current migration history is:

| Provider   | Current history                            |
|------------|--------------------------------------------|
| PostgreSQL | `InitialCreate` → `MoveTablesToAuthSchema` |
| SQL Server | `InitialCreate` → `MoveTablesToAuthSchema` |
| MySQL      | `InitialCreate`                            |

PostgreSQL and SQL Server use the explicit `Auth` schema. MySQL keeps the tables in the configured database because MySQL treats schemas as databases.

When the Auth persistence model changes, a new migration is generated for every affected supported provider. Already-applied migrations are not edited.

See [Documentation/MIGRATIONS.md](Documentation/MIGRATIONS.md) for the complete migration workflow, design-time commands, schema rationale, validation strategy and deployment guidance.

User emails are canonicalized for added and modified `AuthUserEntity` instances on synchronous and asynchronous save paths:

```text
Trim -> ToLowerInvariant -> EF Core persistence
```

This behavior is verified through real database round-trips for all three supported providers.

## Project Structure

```text
KUKULCAN.SharedKernel.Auth/
├── Source/
│   ├── KUKULCAN.SharedKernel.Auth/
│   ├── KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL/
│   ├── KUKULCAN.SharedKernel.Auth.Migrations.SQLServer/
│   └── KUKULCAN.SharedKernel.Auth.Migrations.MySQL/
├── Tests/
│   ├── KUKULCAN.SharedKernel.Auth.UnitTests/
│   ├── KUKULCAN.SharedKernel.Auth.SQLServer.Integration/
│   ├── KUKULCAN.SharedKernel.Auth.PostgreSQL.Integration/
│   └── KUKULCAN.SharedKernel.Auth.MySQL.Integration/
├── .github/workflows/
└── repository documentation
```

## Testing

Unit tests cover local authentication, password hashing, federated services/providers, provider credential validation, multi-tenancy, infrastructure failures and cancellation.

The three integration projects validate real persistence behavior, including authentication, tenant boundaries, query filters, constraints, cascade deletion, canonicalization, cancellation and federated end-to-end flows.

Current `main` validation:

| Test project           |        Result |
|------------------------|--------------:|
| UnitTests              |     130 / 130 |
| PostgreSQL Integration |       42 / 42 |
| SQL Server Integration |       42 / 42 |
| MySQL Integration      |       42 / 42 |
| **Total**              | **256 / 256** |

## TDD Workflow

```text
TEST → RED → Source → GREEN → Coverage → PR → merge
```

Production code is not changed before its corresponding behavior test exists. If a new test documents behavior already guaranteed by production code, immediate GREEN is valid and production code must not be modified merely to manufacture RED.

Coverage is an audit signal, not the objective.

For persistence-model changes, EF Core migration artifacts are generated after the source change reaches GREEN and are validated against the corresponding real database providers before the PR is merged.

## Requirements

- .NET SDK 10.0
- C# latest supported language version
- Nullable Reference Types enabled
- Docker and the corresponding SQL Server, PostgreSQL and MySQL test infrastructure

Production dependencies include `KUKULCAN.SharedKernel` 1.0.0, `KUKULCAN.SharedKernel.Database` 1.0.2, Entity Framework Core 10, Microsoft.Extensions.Identity.Core 10 and System.IdentityModel.Tokens.Jwt 8.23.0.

## Build and Test

```bash
dotnet restore KUKULCAN.SharedKernel.Auth.slnx
dotnet build KUKULCAN.SharedKernel.Auth.slnx --no-restore --configuration Release

dotnet test Tests/KUKULCAN.SharedKernel.Auth.UnitTests/KUKULCAN.SharedKernel.Auth.UnitTests.csproj --no-build --configuration Release
dotnet test Tests/KUKULCAN.SharedKernel.Auth.PostgreSQL.Integration/KUKULCAN.SharedKernel.Auth.PostgreSQL.Integration.csproj --no-build --configuration Release
dotnet test Tests/KUKULCAN.SharedKernel.Auth.SQLServer.Integration/KUKULCAN.SharedKernel.Auth.SQLServer.Integration.csproj --no-build --configuration Release
dotnet test Tests/KUKULCAN.SharedKernel.Auth.MySQL.Integration/KUKULCAN.SharedKernel.Auth.MySQL.Integration.csproj --no-build --configuration Release
```

## Coverage

`.github/workflows/coverage.yml` measures production coverage using Cobertura. It runs UnitTests plus PostgreSQL, SQL Server and MySQL integration tests and publishes one artifact per suite. Test assemblies and compiler-generated code are excluded. No artificial global threshold is used.

See [TESTS-Auditoria-Coverage.md](TESTS-Auditoria-Coverage.md).

## API Boundary

`KUKULCAN.SharedKernel.Auth` remains a class library. A consuming application or separate host owns `Program`, dependency injection composition, HTTP routing, controllers/minimal APIs, HTTP status mapping, transport DTOs and application-level JWT issuance.

No ASP.NET Core host should be introduced into `Source/KUKULCAN.SharedKernel.Auth`.

## Security

Authentication code is security-sensitive. Changes must preserve password-hash confidentiality, token signature/issuer/audience/lifetime validation, stable provider identity mapping, tenant isolation and controlled error handling.

Report vulnerabilities privately according to [SECURITY.md](SECURITY.md).

## Documentation

- [CHANGELOG.md](CHANGELOG.md)
- [Documentation/README.md](Documentation/README.md)
- [Documentation/MIGRATIONS.md](Documentation/MIGRATIONS.md)
- [CONTRIBUTING.md](CONTRIBUTING.md)
- [ROADMAP.md](ROADMAP.md)
- [SECURITY.md](SECURITY.md)
- [SUPPORT.md](SUPPORT.md)
- [TESTS-Auditoria-Coverage.md](TESTS-Auditoria-Coverage.md)

## License

See [LICENSE](LICENSE) for the applicable GNU General Public License terms.
