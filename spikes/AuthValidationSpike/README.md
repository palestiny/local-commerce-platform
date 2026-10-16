# Isolated JWT Validation Spike

**Status: experimental; not production code. No authentication provider selected.**

This project is intentionally outside `LocalCommercePlatform.sln` and has no reference to production Domain/Application/Infrastructure projects.

## Purpose

Exercise ASP.NET Core JWT bearer validation with a small protected diagnostic endpoint. Automated tests use a test-only issuer and signing key to make validation cases deterministic. They do **not** prove integration with Supabase Auth, Auth0, or another real identity provider.

## Configuration

Set these environment variables locally only when testing against a real provider:

- `Authentication__Authority`: the provider's confirmed HTTPS issuer/authority URL.
- `Authentication__Audience`: the API audience/resource identifier expected in access tokens.

Confirm both values from the provider's project/application configuration; do not guess them. Do not commit provider secrets, access tokens, or real tenant credentials.

The automated tests override authentication settings and use an in-memory issuer and test signing key. Never copy the test key or test configuration into a deployed environment.

## Run locally

From the repository root, with the .NET 8 SDK installed:

```powershell
dotnet test .\spikes\AuthValidationSpike.Tests\AuthValidationSpike.Tests.csproj
```

## CI verification

The dedicated workflow is `.github/workflows/auth-validation-spike.yml`. It restores and tests this isolated test project on pushes to `spike/auth-validation` that change spike/workflow files and on matching pull-request changes. It is separate from the production solution and does not make this spike production code.

The deterministic suite covers:
- Anonymous `/health` succeeds.
- Missing and malformed bearer tokens are rejected.
- A valid test-signed token succeeds and returns only the subject identifier.
- Wrong issuer, wrong audience, expired token, and token signed with an untrusted key are rejected.

A green CI run verifies these deterministic local cases only; it does not establish real-provider compatibility. The latest verified CI for commit `3606cca7dd9f49e1b4513b9b171e77b65ee9f5fd` is linked from the [PR](https://github.com/palestiny/local-commerce-platform/pull/19).

## Limitations and next evidence required

- No real provider tenant or provider-issued access token has been tested.
- No login UI, refresh-token flow, logout/revocation, account recovery, or mobile SDK flow is implemented.
- The diagnostic endpoint returns only a validated subject claim; it is not a resource authorization model.
- No production endpoints, account schema, or role/ownership rules are included.
- Before choosing a provider, run a separately configured integration test with provider-issued tokens and document issuer/audience, token claims, expiry behavior, refresh/logout/revocation behavior, and mobile compatibility without committing secrets.
