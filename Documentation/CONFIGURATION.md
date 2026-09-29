# Configuration

## Scope

`KUKULCAN.SharedKernel.Auth` is a class library and does not define an ASP.NET Core configuration section.

The consuming application supplies dependency injection and provider-specific configuration.

## Database

Authentication persistence uses the database configuration supplied by `KUKULCAN.SharedKernel.Database`.

Database provider, connection string, retry, pool and diagnostic settings therefore belong to the shared database configuration contract.

## Local Authentication

Local authentication requires implementations for:

- `ILocalUserStore`;
- `IPasswordHasher`;
- tenant context infrastructure.

The package does not require a particular web-host configuration model.

## Federated Authentication

Provider validators require:

- the provider client identifier;
- an `HttpClient`.

Google, Microsoft and Apple validators retrieve OIDC metadata and JWKS through the supplied HTTP client.

## Dependency Injection

The consuming application composes:

- `AuthDbContext`;
- local and federated stores;
- password hashing;
- provider validators;
- federated authentication providers;
- `ITenantContext`;
- other shared infrastructure dependencies.

## Security

Connection strings, client identifiers where sensitive, and all credentials must be supplied through the application's secret-management mechanism and never committed to source control.

Credential values and tokens must not be logged.

## Provider Metadata

OIDC discovery and signing-key endpoints are part of the provider validation contract. They are not arbitrary application endpoints.

The validator must therefore validate the retrieved issuer and signing keys before accepting a credential.
