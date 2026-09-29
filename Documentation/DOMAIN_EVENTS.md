# Domain Events

## Scope

Authentication entities participate in the domain-event infrastructure supplied by `KUKULCAN.SharedKernel.Database` where the corresponding shared contract is implemented.

The Auth package does not introduce a second event-dispatch mechanism.

## Persistence Relationship

Domain events are dispatched through the shared persistence pipeline after the database persistence operation succeeds.

Authentication-specific state preparation, such as email canonicalization, occurs before the shared pipeline is invoked.

```text
AuthDbContext
 -> authentication state preparation
 -> shared persistence pipeline
 -> successful persistence
 -> domain-event dispatch
```

## Authentication Events

Authentication domain events should represent meaningful state transitions rather than persistence implementation details.

Examples of suitable event concepts include:

- user created;
- tenant membership added or removed;
- federated identity linked or unlinked.

The exact event contract belongs to the authentication domain model and must not be inferred from database row changes alone.

## Failure Semantics

An event must not be treated as successfully persisted before the underlying persistence operation succeeds.

Failure behavior, dispatcher ordering and common event infrastructure are inherited from `KUKULCAN.SharedKernel.Database`.

## Testing

Authentication tests should verify an event only when the event itself is part of the public authentication contract.

Generic dispatch mechanics belong to the Database repository's test suite.
