# Security Policy

**KUKULCAN.SharedKernel.Auth** is security-sensitive infrastructure. Authentication, credential validation and tenant membership handling are security-critical behavior.

## Supported Versions

| Version | Support |
|---|---|
| Latest stable release | Supported |
| Older unsupported releases | Not supported |
| Pre-release versions | Evaluation/testing only |

## Reporting a Vulnerability

Do **not** report security vulnerabilities through public Issues or Discussions.

Use GitHub Private Vulnerability Reporting when enabled. Otherwise, contact the project maintainers privately.

Include, when available, the affected version/commit, .NET version, operating system, database provider/version, description, reproduction steps, proof of concept, security impact and suggested mitigation.

Do not include real passwords, tokens, private keys or production credentials.

## Relevant Security Areas

Reports are especially important for authentication bypass, password verification, credential disclosure, JWT signature/issuer/audience/lifetime validation, provider identity mapping, OIDC/JWKS validation, signing-key handling, tenant isolation, unauthorized membership exposure, persistence constraints and vulnerable dependencies.

## Security Principles

- Password hashes are never returned in authentication results.
- Federated identities use stable provider identifiers, not email addresses.
- Google and Apple use `sub`.
- Microsoft uses `tid:oid`.
- JWT validation checks security-relevant claims and signatures.
- Invalid federated infrastructure input is normalized to `Auth.FederatedCredentialInvalid`.
- Cancellation is not converted into successful authentication.
- Complete memberships are preserved while active-tenant access is enforced.
- Persistence uses the shared tenant-aware database infrastructure.

## Secure Development

Security-sensitive changes follow behavior-first TDD. Tests use deterministic cryptographic material and controlled HTTP responses and must not depend on live provider credentials.

Coverage is an audit signal with particular attention to security-sensitive branches and exception paths.

## Dependencies

Current dependencies include Entity Framework Core 10, Microsoft.Extensions.Identity.Core 10, System.IdentityModel.Tokens.Jwt 8.23.0, KUKULCAN.SharedKernel 1.0.0 and KUKULCAN.SharedKernel.Database 1.0.2.

Dependencies must be evaluated before introduction and kept current where supported.

## Security Response

Maintainers will acknowledge, reproduce and assess the report, identify affected components, develop and test a mitigation, and publish a correction and guidance when appropriate.

Security fixes should include regression tests whenever feasible.
