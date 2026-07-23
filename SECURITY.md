# Security Policy

## Supported Versions

We release patches for security vulnerabilities. Currently supported versions:

| Version | Supported          |
| ------- | ------------------ |
| 3.1.x   | :white_check_mark: |
| 3.0.x   | :white_check_mark: |
| 2.x     | :x: |

## Reporting a Vulnerability

If you discover a security vulnerability within this project, please send an email to thomas@th11s.de.
All security vulnerabilities will be promptly addressed.


**Please do not report security vulnerabilities through public GitHub issues.**

### What to Include

- Type of issue (e.g., buffer overflow, SQL injection, cross-site scripting, etc.)
- Full paths of source file(s) related to the manifestation of the issue
- The location of the affected source code (tag/branch/commit or direct URL)
- Any special configuration required to reproduce the issue
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the issue, including how an attacker might exploit it

### Response Timeline

- We will acknowledge receipt of your vulnerability report within 48 hours
- We will provide a more detailed response within 7 days indicating the next steps
- We will keep you informed about the progress towards a fix and full announcement

## Security Update Process

Security updates will be released as soon as possible after a vulnerability is confirmed and a fix is available. Updates will be announced through:

- GitHub Security Advisories
- Release notes
- Repository README

## Best Practices

When using this ACME Server implementation with Active Directory Certificate Services:

- Always use HTTPS in production environments
- Keep your .NET runtime and dependencies up to date
- Regularly review and update your CA policies
- Follow the principle of least privilege for service accounts
- Monitor logs for suspicious activity
- Keep your ADCS infrastructure secure and properly configured
