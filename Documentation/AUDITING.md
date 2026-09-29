# Auditing

## Scope

Authentication uses the auditing infrastructure supplied by `KUKULCAN.SharedKernel.Database`.

This document describes the authentication-specific boundary; the complete interceptor contract belongs to the Database repository.

## Authentication Entities

Authentication entities participate in shared auditing when they implement the corresponding shared audit contract.

The authentication package does not duplicate the auditing interceptor or timestamp policy.

## Persistence Flow

```text
AuthDbContext
 -> shared database persistence pipeline
 -> auditing infrastructure
 -> EF Core
 -> relational database
```

Authentication-specific email canonicalization occurs before delegation to the shared persistence pipeline.

## Responsibilities

The Auth package is responsible for:

- preserving authentication entity state;
- canonicalizing local-user email values;
- delegating common auditing behavior to the shared infrastructure.

The Database package is responsible for:

- audit timestamp semantics;
- clock abstraction;
- audit interceptor behavior.

## Testing

Authentication integration tests verify persisted authentication state against all supported relational test providers.

Auditing behavior inherited from the database package is tested there and should not be duplicated here unless authentication introduces an additional observable audit contract.
