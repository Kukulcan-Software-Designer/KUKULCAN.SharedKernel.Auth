# Support

**KUKULCAN.SharedKernel.Auth** provides reusable authentication infrastructure for the KUKULCAN ecosystem.

## Before Asking for Help

Please check:

- `README.md`.
- `CONTRIBUTING.md`.
- `SECURITY.md`.
- `CHANGELOG.md`.
- Existing GitHub Issues and Discussions.

Also verify that you are using a current supported version.

## Reporting Bugs

Use GitHub Issues for reproducible defects that are not security vulnerabilities.

Include, when applicable:

- package or repository version;
- .NET version;
- operating system;
- database provider and version;
- complete exception message;
- stack trace;
- expected behavior;
- actual behavior;
- minimal reproduction.

For authentication problems, also describe the authentication mode involved: local, Google, Microsoft or Apple.

Do not include passwords, tokens, client secrets, private keys or other credentials in an issue.

## Architectural Questions

Use GitHub Discussions for questions concerning:

- local authentication;
- federated authentication;
- tenant membership behavior;
- integration with `KUKULCAN.SharedKernel`;
- integration with `KUKULCAN.SharedKernel.Database`;
- database-provider integration;
- testing strategy.

## Feature Requests

Feature requests should explain:

- the problem;
- why the current API is insufficient;
- why the functionality belongs in the authentication shared component;
- the proposed approach;
- relevant security and multi-tenant implications.

## Out of Scope

This project does not provide general support for:

- application-specific business rules;
- database administration;
- unrelated ASP.NET Core configuration;
- third-party identity-provider account administration;
- general C# or .NET troubleshooting.

## Security

Security vulnerabilities must follow [SECURITY.md](SECURITY.md), not public Issues or Discussions.
