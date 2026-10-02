# Persistence Pipeline

## Overview

`AuthDbContext` derives from the persistence infrastructure in `KUKULCAN.SharedKernel.Database`.

Its authentication-specific responsibility is email canonicalization.

```text
AuthDbContext.SaveChanges*
      |
      v
Canonicalize AuthUserEntity.Email
      |
      v
KukulcanDbContextBase
      |
      v
Shared interceptors
      |
      v
EF Core
      |
      v
SQL Server / PostgreSQL / MySQL
```

## Email Canonicalization

For Added and Modified `AuthUserEntity` entries:

```csharp
Email = Email.Trim().ToLowerInvariant();
```

This is applied for synchronous and asynchronous save paths.

## SaveChanges Overloads

Authentication overrides the relevant synchronous and asynchronous `SaveChanges` overloads and delegates persistence to the shared base context after canonicalization.

Cancellation tokens are passed through to the asynchronous EF Core pipeline.

## Shared Pipeline

After authentication-specific preparation, common infrastructure remains owned by `KUKULCAN.SharedKernel.Database`.

Depending on the entity and implemented contracts this includes:

- auditing;
- soft delete;
- immutability;
- domain-event dispatch;
- tenant-aware query/model behavior;
- transaction and unit-of-work infrastructure.

Auth does not duplicate those concerns.

## Tenant Query Filters

Authentication stores deliberately distinguish normal tenant-scoped queries from complete membership reconstruction.

A query that reconstructs all memberships for a known authenticated identity may use `IgnoreQueryFilters()` because the authentication contract requires all memberships.

This is not a general tenant-isolation bypass.

## Persistence Invariants

Authentication persistence includes:

- unique local-user email;
- composite user/tenant membership identity;
- composite provider/subject federated identity;
- required relationships and foreign keys;
- cascade behavior defined by the entity configuration.

## Testing

The unit suite validates deterministic authentication behavior.

The PostgreSQL, SQL Server and MySQL suites validate the real persistence pipeline, relational constraints, tenant behavior and email canonicalization.

## EF Core Migrations

Migrations are part of the persistence deployment boundary, not the domain model itself. The main Auth assembly contains the EF model; provider-specific migration packages contain the generated migration history and design-time factories.

The initial migration creates only the Auth persistence concepts currently owned by the module:

``
Users
TenantMemberships
FederatedIdentities
```

There is intentionally no Tenant catalog table. `TenantMemberships.TenantId` remains an identifier reference to the tenant owned by the consuming application.

Pending migrations can be applied by a consuming application with:

``
csharp
await dbContext.Database.MigrateAsync(cancellationToken);
```

See [MIGRATIONS.md](MIGRATIONS.md) for provider-specific design-time commands.
