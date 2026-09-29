# Architecture Decisions

## ADR-001 — Authentication Remains a Class Library

**Status:** Accepted

Authentication is shared infrastructure, not an HTTP application. The package therefore does not contain controllers, endpoints or host-specific authorization policies.

## ADR-002 — Reuse KUKULCAN.SharedKernel.Database

**Status:** Accepted

Authentication persistence uses the shared EF Core infrastructure instead of introducing another persistence abstraction.

This keeps provider-specific persistence and common cross-cutting behavior in the database package.

## ADR-003 — Separate Federated Validation from Authentication

**Status:** Accepted

OIDC/JWT validation is isolated behind provider credential validators. Authentication orchestration then resolves the validated external identity to a local user.

This prevents provider-specific protocol details from leaking into the authentication service.

## ADR-004 — Stable External Subjects

**Status:** Accepted

Federated identities use provider-specific stable subjects:

- Google: `sub`
- Microsoft: `tid:oid`
- Apple: `sub`

Email is not used as the federated identity key.

## ADR-005 — Preserve Complete Tenant Memberships

**Status:** Accepted

Successful authentication returns all memberships while separately checking access to the active tenant.

Changing this behavior to return only the current tenant would change the authentication contract.

## ADR-006 — Canonicalize Email at Persistence

**Status:** Accepted

Added and modified local-user email values are persisted as:

```text
Email.Trim().ToLowerInvariant()
```

This keeps lookup and persisted identity consistent.

## ADR-007 — Real Provider Integration Tests

**Status:** Accepted

Authentication persistence is tested against PostgreSQL, SQL Server and MySQL because relational constraints, query filters and persistence behavior should be verified against real database engines.

## ADR-008 — Coverage Is an Audit Signal

**Status:** Accepted

Tests are added for behavior, security and regression contracts. Tests are not created solely to increase a coverage percentage.
