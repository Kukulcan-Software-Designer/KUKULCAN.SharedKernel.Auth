# Tenancy

## Purpose

Authentication tenancy has two separate concerns:

1. determine whether the authenticated user can access the active tenant;
2. return the complete set of memberships belonging to that user.

These concerns must not be collapsed.

## Membership Model

`AuthTenantMembershipEntity` associates a local user with a tenant.

The user/tenant pair forms the membership identity.

## Authentication Contract

```text
No permitted active-tenant access
    -> Auth.NoTenantAccess

Permitted active-tenant access
    -> Success + ALL user memberships
```

The active tenant is therefore an access condition, not a filter on the final authenticated-user membership collection.

## Tenant Query Filters

Normal persistence queries are subject to the tenant-aware filtering supplied by the shared Database infrastructure.

Authentication stores have a deliberate exception when reconstructing a specific user's complete membership set.

The sequence is conceptually:

```text
Find authentication identity
 -> retrieve complete memberships for that identity
 -> evaluate active-tenant access
 -> return success or Auth.NoTenantAccess
```

The use of `IgnoreQueryFilters()` for complete membership reconstruction must not be interpreted as permission to query arbitrary users.

## Isolation

The membership collection returned by authentication belongs only to the resolved local user.

Authentication must not:

- inherit another user's memberships;
- treat membership in any tenant as active-tenant access;
- discard non-active memberships after successful authentication.

## Tenant Switching

Changing the active tenant changes the access context. It does not mutate the persisted membership collection.

A user belonging to tenants A, B and C continues to have those memberships available after switching the active context among them.

## Provider Independence

Local credentials and Google, Microsoft and Apple identities converge on the same local-user membership model.

Tenant behavior is therefore independent of the external identity provider.

## Testing

The three integration suites validate:

- no membership;
- active-tenant access;
- denied active-tenant access;
- complete membership retrieval;
- tenant switching;
- user isolation;
- membership persistence;
- local and federated authentication.

Any change to the distinction between active-tenant access and complete membership enumeration must begin with a behavior test.
