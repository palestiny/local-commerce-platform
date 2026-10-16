# Authentication Integration Spike — Execution Plan

Status: **PROPOSED SPIKE — NO PROVIDER SELECTED — PRODUCTION AUTH/API BLOCKED**

Date: 2026-10-16

## Purpose

Gather implementation evidence before selecting authentication for the Local Commerce Platform. The current solution targets .NET 8 and is a modular monolith with Domain, Application, and Infrastructure projects. The solution file currently contains no dedicated API/HTTP project or API test project, so this spike must not silently introduce a production HTTP layer.

Compare:
- **A — ASP.NET Core Identity:** estimate and verify the minimum viable secure account/session design needed for web and native mobile. Identity alone is not assumed to provide a standard JWT issuer or complete mobile refresh/revocation lifecycle.
- **B1 — Supabase Auth:** validate a real provider-issued access token in ASP.NET Core.
- **B2 — Auth0:** validate a real provider-issued access token in ASP.NET Core.

This is a disposable, isolated proof of concept. It must not be merged into the production application or treated as the selected architecture.

## Scope and non-goals

### In scope
- A minimal isolated .NET 8 HTTP test host or spike project, separated from Domain/Application business code.
- One protected diagnostic endpoint that returns only a stable test response and the server-derived subject identifier; it must never return token contents or credentials.
- Authentication tests for missing, malformed, expired, wrong-issuer, wrong-audience, and invalid-signature credentials.
- A short web-client and native-mobile flow assessment based on official current provider documentation.
- A comparable record of setup effort, required configuration, operational responsibilities, exit/migration concerns, and provider-specific constraints.

### Out of scope
- Production controllers or endpoints for Orders.
- Production user/account tables, migrations, authorization roles, merchant/store membership, or driver/dispatch permissions.
- Production login UI, password recovery UI, social-login implementation, or production secrets.
- Any decision to move business data from PostgreSQL or to use provider SDKs inside Domain/Application.
- Treating a role claim as permission to access a specific Order, Store, or Delivery.

## Spike isolation and safety

1. Keep the spike in a separate project/branch. Do not reference it from the production solution unless a later owner-approved design explicitly requires that.
2. Use local environment variables or the .NET user-secrets mechanism for issuer/domain, audience, and test credentials. Never commit secrets or copy access/refresh tokens into logs, issues, or test fixtures.
3. Do not ask the owner to paste credentials into chat. If a real tenant is needed, provide the exact configuration-key names and have the owner configure them locally.
4. No live customer data. Use test accounts only.
5. Use the same diagnostic response and acceptance tests for each managed provider. Provider-specific validation must use official libraries/configuration and validate signature, issuer, audience, expiry, and allowed algorithms as applicable.
6. The diagnostic endpoint is not an authorization model. It must not be exposed in production or merged as a customer API.

## Test matrix

| Case | Expected result | Evidence |
|---|---|---|
| No bearer credential | 401; protected handler not executed | Automated HTTP test |
| Malformed bearer credential | 401; no token detail leaked | Automated HTTP test |
| Expired access token | 401 | Automated test with deterministic test token or provider-issued expired test token |
| Wrong issuer | 401 | Automated test |
| Wrong audience | 401 | Automated test |
| Invalid signature / untrusted signing key | 401 | Automated test |
| Valid token from the configured test issuer | 200; only server-validated subject is returned | Automated HTTP test plus a real provider-issued test token |
| Provider A token presented to Provider B configuration | 401 | Automated test where practical |
| Token validation configuration missing or invalid | Application fails closed; no anonymous fallback | Startup/configuration test |
| Sensitive values in logs | No bearer token, refresh token, secret, or raw credential | Log-capture test/review |

For issuer/JWKS rotation and key refresh behavior, document what is proven by library tests versus what requires a provider-backed integration check. Do not claim deterministic coverage of remote key rotation unless the test actually exercises it.

## Required comparison record

For A, B1, and B2, record:
- Exact .NET packages and versions used, and the official documentation used.
- Web sign-in/session flow and native Android/iOS sign-in/refresh flow.
- Token validation and signing-key discovery configuration.
- Expiry, refresh, logout, revocation, and multi-device session behavior; clearly label behavior as provider-managed, application-owned, or not proven.
- Account verification and recovery capabilities required for the pilot.
- Current pricing/limits, regional/data handling terms, and plan dependencies from official sources; do not infer production suitability from a free tier alone.
- Estimated engineering work as a range with assumptions, not a false-precision point estimate.
- Operational burden, outage/dependency risk, portability/exit plan, and unresolved questions.
- A clear separation between authentication (who is signed in) and application authorization (which resources they may access).

## Acceptance criteria

The spike is complete only when:
- The same automated negative-token matrix passes for each implemented managed-provider adapter.
- A valid provider-issued test token is verified end-to-end for each provider being compared.
- The ASP.NET Core Identity alternative has an explicit, documented plan for standard client token/session behavior; built-in proprietary Identity API tokens are not described as standard JWTs.
- Web and native-mobile flows are assessed for all candidates against current official documentation.
- Setup/implementation effort and configuration are recorded, including blockers and any test that could not be completed.
- No secret is committed, and the spike has no production business endpoints.
- The comparison concludes with a recommendation and trade-offs, but **does not choose a provider on behalf of the owner**.

## Exit decision

After reviewing the evidence, the owner chooses one of:
- **A:** ASP.NET Core Identity plus an explicitly designed session/token mechanism;
- **B1:** Supabase Auth;
- **B2:** Auth0;
- **DEFER:** name the missing budget, hosting, product, or legal fact and the date/condition for revisiting it.

Only after the owner decision may a production authentication design be drafted. The approved initial HTTP scope remains limited to:
- `POST /api/v1/orders`
- `GET /api/v1/orders/{orderId}`
- `POST /api/v1/orders/{orderId}/cancel`

This spike does not close the wider API/security gate. DTO/status/header semantics, idempotency contract, error mapping, operational limits, audit, OpenAPI, and pilot scope still require evidence. Production HTTP implementation remains blocked until the gate exit criteria are satisfied.

## Current result

**NOT RUN.** This document defines the spike and acceptance criteria only. No provider tenant has been configured, no runtime code has been added, and no tests are claimed to have run.


## Official technical references checked — 2026-10-16

- Microsoft Learn, ASP.NET Core 8 JWT bearer configuration: https://learn.microsoft.com/ar-sa/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-8.0
  - The API must validate token signature, expected issuer, expected audience, and expiration; invalid credentials should be rejected with 401. This is the baseline acceptance contract, not a provider-specific implementation recipe.
- Supabase, JWT guidance: https://supabase.com/docs/guides/auth/jwts
  - Supabase documents project JWKS for asymmetric signing keys and advises using a reputable verification library rather than implementing JWT verification manually. The spike must account for the project's actual signing-key mode.
- Auth0, ASP.NET Core API Authentication library: https://auth0.github.io/aspnetcore-api/index.html
  - The official library documents .NET 8 support and wraps JWT bearer authentication with Auth0-specific configuration. Package choice/version and compatibility must still be pinned and verified in the spike.

These references inform the test plan only. They do not establish that either provider has been integrated, that a tenant is configured, or that any test has passed.


## Verification update — 2026-10-16

The isolated deterministic JWT validation test host has now been implemented on the separate branch `spike/auth-validation`. CI passed for commit `3606cca7dd9f49e1b4513b9b171e77b65ee9f5fd`:
- [Authentication spike PR workflow](https://github.com/palestiny/local-commerce-platform/actions/runs/38858117540)
- [Authentication spike push workflow](https://github.com/palestiny/local-commerce-platform/actions/runs/38858114010)
- [Main solution build/test and migration/recovery workflow](https://github.com/palestiny/local-commerce-platform/actions/runs/38858117552)

The automated suite covers anonymous health, missing/malformed bearer credentials, a valid test-signed token, wrong issuer, wrong audience, expiration, and an untrusted signing key. The diagnostic response is asserted to contain only the subject identifier.

**Interpretation:** this closes the local JWT-validation test-host milestone only. It does not close the provider integration spike acceptance criteria: no Supabase/Auth0 tenant or provider-issued token has been tested; refresh/logout/revocation, mobile flows, provider-specific claim mapping, key discovery/rotation, and operational behavior remain unverified. ASP.NET Core Identity comparison is also still documentary, not an implemented or benchmarked alternative. No provider is selected and production HTTP/auth implementation remains blocked.
