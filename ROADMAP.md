# Roadmap

## Purpose

This roadmap describes the intended evolution of **KUKULCAN.SharedKernel.Auth**. It communicates direction rather than fixed delivery dates.

Architectural correctness, security, testability and backward compatibility take precedence over feature volume.

## Current Status

The repository contains the authentication domain contracts, local authentication implementation, persistence integration and the test specifications for federated provider validation.

The current authentication scope includes:

- local database authentication;
- multi-tenant authentication results;
- Google federated authentication contracts;
- Microsoft federated authentication contracts;
- Apple federated authentication contracts;
- provider-specific persistence;
- SQL Server, PostgreSQL and MySQL integration test suites;
- NUnit unit and integration test specifications.

## Completed Foundation

### Local Authentication

- Local credential validation.
- Password hashing and verification.
- User persistence.
- Tenant membership persistence.
- Retrieval of all tenant memberships during authentication.
- Tenant-aware persistence behavior.
- SQL Server integration.
- PostgreSQL integration.
- MySQL integration.

### Federated Identity Persistence

- Federated identity model.
- Composite provider/subject identity key.
- User association.
- Tenant membership retrieval.
- SQL Server, PostgreSQL and MySQL persistence tests.

### Provider Contracts

- Google provider contract.
- Microsoft provider contract using `tid:oid` as the external subject.
- Apple provider contract.
- Federated authentication service and provider-wrapper tests.

## Next Development Stage

The next implementation stage is provider credential validation:

1. Google credential validation.
2. Microsoft credential validation.
3. Apple credential validation.

The existing validator tests define the required validation behavior for:

- trusted signatures;
- issuer;
- audience;
- expiration;
- required identity claims;
- provider-specific subject mapping.

## Future Authentication API

**KUKULCAN.SharedKernel.Auth is a class library and does not host an ASP.NET Core application.**

The authentication API is an application-level HTTP boundary that consumes this library. The Web API host, routing, HTTP status mapping and public transport contract therefore belong to the consuming application or to a separate API host project.

Future work includes:

- HTTP authentication endpoints implemented outside the class library;
- consistent mapping of authentication results to HTTP responses;
- JWT issuance and validation at the application/API boundary;
- API-level multi-tenant authentication responses;
- documentation of the public HTTP contract.

API tests must execute against a real ASP.NET Core host. They must not be placed in the library's unit-test project or introduce a `Program`/Web API host into `Source/KUKULCAN.SharedKernel.Auth`.

The API layer should be implemented only after its executable HTTP contract has been defined by tests in the project that owns the API host.

## Provider Integration

Future provider work may include:

- production configuration for Google, Microsoft and Apple;
- secure key discovery and rotation handling;
- provider-specific configuration documentation;
- operational diagnostics without exposing credentials or tokens.

## Quality Goals

Continue to maintain:

- deterministic unit tests;
- provider-specific integration tests;
- real database integration coverage;
- tenant-isolation tests;
- security regression tests;
- coverage auditing based on behavior rather than artificial coverage targets.

## Architectural Boundaries

The project should remain focused on authentication infrastructure.

It should not become:

- a general authorization framework;
- an application-specific user-management system;
- a generic repository framework;
- a replacement for `KUKULCAN.SharedKernel.Database`;
- a general-purpose identity provider.

New functionality should be promoted into the shared authentication component only when it represents a genuine cross-application requirement.
