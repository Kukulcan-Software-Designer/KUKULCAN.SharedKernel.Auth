# KUKULCAN.SharedKernel.Auth Architecture

## Purpose

`KUKULCAN.SharedKernel.Auth` is a reusable authentication library for local and federated authentication in multi-tenant applications.

It provides authentication behavior and persistence integration without becoming an HTTP API or application host.

## Responsibilities

- Local email/password authentication.
- Password hashing and verification.
- Local user persistence.
- Tenant membership resolution.
- Federated identity linking.
- Google, Microsoft and Apple credential validation.
- Authentication result composition.
- Authentication-specific persistence rules such as email canonicalization.

The consuming application remains responsible for HTTP endpoints, access tokens and authorization policies.

## Main Boundaries

```text
Consuming application
        |
        v
KUKULCAN.SharedKernel.Auth
   |                 |
   v                 v
Shared Database   External IdPs
   |
   v
SQL Server / PostgreSQL / MySQL
```

Authentication persistence is built on `KUKULCAN.SharedKernel.Database`.

## Authentication Flows

### Local

```text
Credentials
 -> LocalAuthenticationService
 -> LocalUserStore
 -> password verification
 -> active-tenant access check
 -> AuthenticatedUser
```

### Federated

```text
Credential
 -> provider validator
 -> FederatedIdentity
 -> linked local user
 -> active-tenant access check
 -> AuthenticatedUser
```

Provider validators own external credential validation. Authentication services own local identity resolution and tenant access.

## Tenant Semantics

A successful authentication result contains **all memberships** of the authenticated user.

The active tenant determines whether access is permitted; it does not reduce the returned membership collection to the active tenant.

No active-tenant access results in `Auth.NoTenantAccess`.

## Persistence Boundary

`AuthDbContext` derives from the shared database context and adds authentication-specific email canonicalization before persistence:

```text
Added / Modified AuthUserEntity
 -> Trim()
 -> ToLowerInvariant()
 -> shared persistence pipeline
 -> database
```

Common auditing, soft-delete, immutability, domain-event and unit-of-work infrastructure is inherited from `KUKULCAN.SharedKernel.Database` where applicable.

## Federated Identity

Provider-specific stable subjects are persisted rather than using email as the external identity key:

| Provider  | Subject   |
|-----------|-----------|
| Google    | `sub`     |
| Microsoft | `tid:oid` |
| Apple     | `sub`     |

Microsoft therefore keeps the external object identity tenant-qualified.

## Security Boundary

Authentication establishes identity and active-tenant access. It does not replace application authorization.

Credential validation must verify issuer, audience, lifetime, signature and supported algorithms before accepting an external identity.
