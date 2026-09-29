# KUKULCAN.SharedKernel.Auth Documentation

This directory contains the technical documentation for the authentication library.

## Architecture

- [ARCHITECTURE.md](ARCHITECTURE.md) — authentication boundaries, local/federated flows and tenant semantics.
- [ARCHITECTURE_DECISIONS.md](ARCHITECTURE_DECISIONS.md) — accepted architectural decisions and their rationale.

## Authentication and Persistence

- [CONFIGURATION.md](CONFIGURATION.md) — authentication and provider configuration boundaries.
- [PERSISTENCE_PIPELINE.md](PERSISTENCE_PIPELINE.md) — authentication-specific persistence processing and integration with the shared database pipeline.
- [TENANCY.md](TENANCY.md) — tenant membership, active-tenant access and tenant isolation.
- [DOMAIN_EVENTS.md](DOMAIN_EVENTS.md) — participation in the shared domain-event pipeline.
- [AUDITING.md](AUDITING.md) — authentication participation in shared auditing.
- [IMMUTABILITY.md](IMMUTABILITY.md) — authentication participation in shared immutability rules.

## Testing and Coverage

- [TESTING.md](TESTING.md) — unit and real-provider integration testing strategy.
- [COVERAGE.md](COVERAGE.md) — coverage scope and interpretation.

## Deliberately Not Duplicated

The following Database documentation is not duplicated as a standalone Auth document because Auth does not own an independent implementation of these concerns:

- [SOFT_DELETE.md](SOFT_DELETE.md)
- [UNIT_OF_WORK.md](UNIT_OF_WORK.md)

Soft delete and unit-of-work behavior are inherited from `KUKULCAN.SharedKernel.Database` and documented authoritatively there.

## Documentation Rule

Authentication-specific behavior belongs here. Shared persistence infrastructure remains documented in the Database repository.
