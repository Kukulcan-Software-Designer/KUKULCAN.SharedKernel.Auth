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
