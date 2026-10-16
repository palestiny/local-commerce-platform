# Isolated JWT Validation Spike

**Status: experimental; not production code. No provider selected.**

This project is intentionally outside `LocalCommercePlatform.sln` and has no reference to production Domain/Application/Infrastructure projects.

## Purpose

Exercise ASP.NET Core JWT bearer validation with a small protected diagnostic endpoint. The local automated tests use a test-only signing key and issuer so negative cases can be deterministic. They do **not** prove integration with Supabase Auth or Auth0.

## Configuration

Set these environment variables locally when testing against a real provider:

- `Authentication__Authority`: provider's HTTPS issuer/authority URL.
- `Authentication__Audience`: API audience/resource identifier expected in access tokens.

Do not put real provider secrets or tokens in this repository. Provider-specific audience and issuer values must be confirmed from the provider's project/application configuration; do not guess them.

The test app uses an in-memory issuer and signing key. Never copy its key or configuration into a deployed environment.

## Run

From this directory's parent, with .NET 8 SDK installed:

```powershell
dotnet test .\spikes\AuthValidationSpike.Tests\AuthValidationSpike.Tests.csproj
```

The spike is not added to the production solution or workflow. CI execution must be wired deliberately after review.

## Limitations

- No real Supabase/Auth0 tenant or provider-issued token has been tested.
- No login UI, refresh-token flow, logout/revocation, account recovery, or mobile SDK flow is implemented.
- The diagnostic endpoint returns only a validated subject claim and is not a resource authorization model.
- No production endpoints, account schema, or role/ownership rules are included.
