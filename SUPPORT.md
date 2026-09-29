# Support

**KUKULCAN.SharedKernel.Auth** provides reusable authentication infrastructure for the KUKULCAN ecosystem.

## Before Asking for Help

Review [README.md](README.md), [CONTRIBUTING.md](CONTRIBUTING.md), [SECURITY.md](SECURITY.md), [CHANGELOG.md](CHANGELOG.md), [ROADMAP.md](ROADMAP.md), [TESTS-Auditoria-Coverage.md](TESTS-Auditoria-Coverage.md) and existing Issues/Discussions.

Verify that the problem is reproducible on a supported .NET 10 environment.

## Reporting Bugs

Use GitHub Issues for reproducible defects that are not security vulnerabilities.

Include, when applicable:

- repository commit or package version;
- .NET SDK version;
- operating system;
- database provider/version;
- complete exception and stack trace;
- expected and actual behavior;
- minimal reproduction.

Identify the authentication mode: local, Google, Microsoft or Apple.

Never include passwords, tokens, client secrets, private keys or connection-string credentials.

## Architectural Questions

Use GitHub Discussions for local/federated authentication, tenant membership behavior, credential validation, KUKULCAN.SharedKernel integration, KUKULCAN.SharedKernel.Database integration, SQL Server/PostgreSQL/MySQL integration, TDD/test architecture and API-host boundaries.

## Feature Requests

Explain the problem, desired behavior, why the current API is insufficient, why it belongs in the shared authentication component, and its security, multi-tenant and compatibility implications.

New behavior should normally be specified with an executable test before production implementation.

## Out of Scope

This project does not provide general support for application-specific business rules, application-specific user-management workflows, database administration, unrelated ASP.NET Core configuration, third-party identity-provider account administration or general C#/.NET troubleshooting.

The library is not an HTTP API host.

## Security

Security vulnerabilities must follow [SECURITY.md](SECURITY.md), not public Issues or Discussions.
