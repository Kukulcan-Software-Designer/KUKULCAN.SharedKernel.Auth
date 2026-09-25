# Security Policy

**KUKULCAN.SharedKernel.Auth** is security-sensitive infrastructure. Authentication, credential validation and tenant membership handling must be treated as security-critical behavior.

## Supported Versions

Only actively maintained releases receive security fixes.

| Version                    | Support                 |
|----------------------------|-------------------------|
| Latest stable release      | Supported               |
| Older unsupported releases | Not supported           |
| Pre-release versions       | Evaluation/testing only |

The exact support window may change with project releases.

## Reporting a Vulnerability

Do **not** report security vulnerabilities through public GitHub Issues or Discussions.

Use GitHub Private Vulnerability Reporting when it is enabled for the repository. Otherwise, contact the project maintainers privately.

The report should include:

- affected version;
- .NET version;
- operating system;
- detailed description;
- reproduction steps;
- proof of concept, when available;
- security impact;
- suggested mitigation, when available.

## Relevant Security Areas

Reports are especially important when they affect:

- authentication bypass;
- credential or password verification;
- JWT or identity-token validation;
- signature validation;
- issuer or audience validation;
- token lifetime validation;
- provider identity handling;
- tenant isolation;
- unauthorized tenant membership exposure;
- sensitive credential disclosure;
- insecure persistence behavior;
- dependency vulnerabilities.

## Responsible Disclosure

Please:

- keep the vulnerability confidential while it is investigated;
- avoid public disclosure before a fix is available;
- provide maintainers reasonable time to investigate and remediate the issue;
- coordinate public disclosure when appropriate.

## Security Response

The maintainers will:

1. Acknowledge the report.
2. Reproduce and assess the issue.
3. Identify affected versions and components.
4. Develop and test a mitigation or fix.
5. Release and document the correction when appropriate.

Security fixes should include regression tests whenever feasible.

## Security Principles

The project follows these principles:

- credentials are never returned as part of an authenticated-user result;
- password hashes are not exposed by authentication responses;
- stable external provider identifiers are used instead of email addresses as federated identity keys;
- tenant memberships are handled explicitly;
- provider token validation must verify the security-relevant token claims and signature;
- persistence must preserve tenant isolation without preventing authentication from retrieving a user's complete membership set.

## Dependencies

Security-sensitive dependencies should be kept current and evaluated before introduction.

Do not add a dependency merely for convenience when an existing .NET or KUKULCAN abstraction is sufficient.
