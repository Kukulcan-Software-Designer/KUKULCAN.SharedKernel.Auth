# Immutability

## Scope

Immutability is supplied by `KUKULCAN.SharedKernel.Database`.

The Auth package does not implement a second immutability interceptor.

## Authentication Implications

An authentication entity that implements the shared immutable contract is subject to the common persistence rule.

The authentication layer must therefore avoid modifying immutable entities after their immutable state has been established.

Mutable authentication state, such as tenant membership changes or other explicitly mutable relationships, must remain modeled as mutable entities.

## Pipeline

```text
AuthDbContext
 -> shared immutability enforcement
 -> EF Core persistence
```

Authentication-specific email canonicalization occurs before delegation to the shared pipeline.

## Testing

Provider-independent immutability mechanics are tested in the Database repository.

Auth integration tests should cover an immutability rule only when an authentication entity introduces an authentication-specific observable invariant.
