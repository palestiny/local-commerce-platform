# Customer Authentication and First API Slice — Design Gate

Status: **PROPOSED — OWNER DECISION REQUIRED BEFORE PROVIDER-SPECIFIC IMPLEMENTATION**

## Objective

Move the customer app from demo-only UI toward a real API without coupling business logic to an identity vendor or exposing customer data prematurely.

## Current verified facts

- The repository has Domain, Application, and Infrastructure projects, but no ASP.NET Core HTTP API host in the solution yet.
- The mobile app uses Expo/React Native and its current authentication screen explicitly states that sign-in is not connected.
- Order creation accepts a customer ID in its application command. That value is not proof of identity and must only be populated from a verified server-side principal at the HTTP boundary.
- New orders now persist nullable CustomerId. Legacy orders remain unowned until verified ownership data is available.
- The customer order-details application handler already has an authorization port, but there is no HTTP authentication adapter or production identity provider configured.

## Identity provider options

| Option | Advantages | Costs / risks | Fit |
|---|---|---|---|
| Supabase Auth | Fast hosted setup; mobile-friendly session flow; JWTs can be validated by the .NET API | Vendor dependency; project configuration and issuer/audience/JWKS validation required; user-to-customer mapping still needed | **Recommended for a fast pilot if the owner accepts hosted identity** |
| ASP.NET Core Identity + token/session service | Strong control over account lifecycle and data; keeps identity operations within the .NET platform | More implementation and operational work: credential flows, token lifecycle, recovery, email verification, abuse protection and deployment hardening | Better if owning the full identity platform is a strategic requirement |
| Managed identity provider (for example, Auth0 or Clerk) | Mature hosted authentication and account flows | Vendor-specific integration and possible usage-based costs; provider choice and pricing need validation | Viable alternative if required features justify it |

No provider is selected by this document. The recommendation is conditional, not an approved architecture change.

## Non-negotiable security contract

1. Validate access-token signature, issuer, audience, expiry and relevant claims on the server.
2. Resolve the external identity subject to an internal customer ID using a server-controlled mapping.
3. Never accept a request-body customer ID, URL customer ID, or client-supplied header as proof of identity.
4. Authorize every protected operation against resource ownership, not merely a role claim.
5. Return a concealed 404 for both missing and not-owned customer orders, as specified in the API security gate.
6. Keep provider SDKs and claims mapping in the API/infrastructure boundary; Domain and Application remain provider-agnostic.
7. Keep access/refresh tokens out of logs and use platform-appropriate secure session storage in the mobile app.
8. Legacy orders with null CustomerId must not be exposed to customers until verified ownership backfill exists.
9. Map typed application errors to RFC 9457 Problem Details with stable codes and trace IDs; never expose stack traces, SQL or provider internals.

## First API vertical slice

### Required host foundation
- Add a dedicated `LocalCommerce.Api` ASP.NET Core host to the solution.
- Configure database access through environment/configuration; fail safely when required production configuration is absent.
- Add health/readiness endpoints that do not leak connection strings or secrets.
- Use versioned routes under `/api/v1`, transport DTOs, bounded request sizes, trace IDs and explicit error mapping.
- Add API-level tests for route contracts and error behavior.

### Safe implementation order
1. Add the API host and an unauthenticated health endpoint; prove the app/API deployment boundary without exposing customer data.
2. Add a public catalog read only after the response DTO and visibility rules are defined. Current persisted Product rows are commerce products, not a complete model of all local businesses/services.
3. Implement identity verification and internal customer mapping using the owner-approved provider.
4. Implement authenticated customer order details and create-order routes, deriving CustomerId from the authenticated principal.
5. Add cross-customer authorization tests, idempotency contract tests and mobile integration; do not mark demo flows as production until these pass.

## Gate decisions required

- Identity provider: Supabase Auth / ASP.NET Core Identity-based solution / another managed provider.
- Account identifier mapping: external subject mapped to an internal Customer record (recommended), with lifecycle and uniqueness constraints.
- Pilot scope: customer commerce orders only, or broader local-service requests in a later slice.
- Deployment target and secret/configuration management, to be decided before production launch.

## Exit criteria

- Owner decision is recorded.
- API host is included in the solution and its configuration/security boundary is tested.
- Identity subject maps deterministically to an internal customer ID.
- No client-supplied identity is trusted.
- Cross-customer reads are denied and concealed consistently.
- API contract, security and mobile integration tests pass before the PR is promoted out of Draft.
