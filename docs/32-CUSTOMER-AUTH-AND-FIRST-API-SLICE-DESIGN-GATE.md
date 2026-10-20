# Customer Authentication and First API Slice — Design Gate

Status: **OWNER DECISIONS RECORDED — DESIGN REVIEW REQUIRED; IMPLEMENTATION NOT AUTHORIZED**

## Objective

Move the customer app from demo-only UI toward a real API while keeping business logic independent of authentication protocol details and preventing unauthorized access to customer data.

## Verified baseline

- The solution currently contains Domain, Application, and Infrastructure projects; no ASP.NET Core HTTP API host is present in the reviewed integration branch.
- Existing projects target .NET 8. The mobile app uses Expo/React Native, and its authentication UI is demo-only.
- Order creation accepts a customer ID in its application command. That value is not proof of identity and must be derived from a verified server-side principal at the HTTP boundary.
- New orders persist nullable CustomerId. Legacy orders remain unowned until verified ownership data is available.
- The customer order-details application handler has an authorization port, but no HTTP authentication adapter or production identity provider is configured.
- The integration PR remains Draft. No API/auth implementation is authorized by this design-gate update.

## Owner-approved decisions

| Decision | Approved direction | Boundary / rationale |
|---|---|---|
| Account management | ASP.NET Core Identity | Own account lifecycle within the .NET platform; do not introduce a hosted identity vendor. |
| Token and protocol layer | ASP.NET Core Identity + OpenIddict | Use OpenIddict for OAuth 2.0 / OpenID Connect protocol endpoints and token issuance rather than treating Identity itself as a complete authorization server. Exact supported grants and client registration remain part of detailed design. |
| Target framework direction | .NET 10 LTS, subject to dependency compatibility review | Review EF Core, PostgreSQL provider, test infrastructure, CI images/actions, and all projects before changing target frameworks. This decision does not authorize a blind or partial upgrade. |
| First API slice | Health endpoint first, then customer commerce-order API only | Do not include public catalog, broader local-service requests, merchant, driver, admin, payment, or other endpoints in this first slice. |

## Decisions still open before implementation

1. **Customer identity mapping and lifecycle:** define the internal customer record and unique mapping from the authenticated Identity/OpenIddict subject to that record; specify first-login provisioning, account deletion/disablement, and behavior when the mapping is absent. Recommended: use a server-controlled, unique mapping and never derive the internal customer ID from user-supplied input.
2. **OAuth/OIDC detailed security profile:** confirm supported flows for native mobile clients, public-client registration, PKCE, redirect URI policy, issuer/audience values, signing-key management and rotation, access-token lifetime, refresh-token rotation/revocation, logout semantics, and development versus production settings. Do not invent a custom OAuth flow.
3. **Persistence and migrations:** decide how ASP.NET Core Identity tables and OpenIddict stores fit the existing PostgreSQL setup, migration ownership/order, schema conventions, and deployment-time migration policy. Review compatibility before upgrading to .NET 10.
4. **Account recovery and abuse controls:** define email confirmation, password reset, rate limiting/lockout, verification delivery provider, and required configuration. Provider and operational cost are not yet approved.
5. **Deployment and secrets:** choose API hosting, public issuer/base URL, TLS termination, secret storage, database credentials, signing certificate/key storage and rotation, health/readiness behavior, and environment separation before production.
6. **Order API contract:** confirm which existing order fields are safe to expose, pagination/detail behavior, status/error mapping, and create-order idempotency. Legacy orders with null CustomerId must remain invisible to customer endpoints until verified ownership backfill exists.

## Non-negotiable security contract

1. Validate access-token signature, issuer, audience, expiry, and required claims at the API.
2. Resolve the authenticated subject to an internal customer ID using a server-controlled mapping.
3. Never accept a body customer ID, URL customer ID, or client-supplied header as proof of identity.
4. Authorize every protected operation against resource ownership, not merely a role claim.
5. Conceal missing and not-owned customer orders consistently (404) where the API contract requires it.
6. Keep identity/protocol adapters at the API/infrastructure boundary; Domain and Application remain independent of Identity/OpenIddict types.
7. Keep access/refresh tokens out of logs and use platform-appropriate secure storage in the mobile app.
8. Do not expose legacy orders with null CustomerId to customers until verified ownership backfill exists.
9. Map typed application errors to RFC 9457 Problem Details with stable error codes and trace IDs; never expose stack traces, SQL, secrets, or protocol-provider internals.
10. Use standards-based native-app authorization with PKCE where applicable; do not embed a client secret in the mobile app.

## First API vertical slice

### Slice 1 — API host and health

- Add a dedicated `LocalCommerce.Api` ASP.NET Core host to the solution only after the .NET 10 dependency compatibility review is complete.
- Configure database access through environment/configuration; fail safely when required production configuration is absent.
- Add health and readiness endpoints that do not leak connection strings, secrets, or sensitive infrastructure details.
- Use versioned routes under `/api/v1`, explicit transport contracts, bounded request sizes, trace IDs, and stable error mapping where applicable.
- Add API-level tests for route contracts and health/readiness behavior.
- No customer-data endpoints are included in this slice.

### Slice 2 — Authenticated customer commerce orders

- Add Identity account lifecycle and OpenIddict protocol configuration according to the approved security profile.
- Resolve the authenticated subject to an internal customer record server-side.
- Implement only customer commerce-order read/detail and create routes after confirming the existing application contracts and persistence behavior.
- Derive CustomerId from the authenticated principal and internal mapping; ignore or reject client-supplied identity fields.
- Add tests for unauthenticated requests, invalid/expired tokens, missing identity mapping, cross-customer access, hidden legacy orders, create-order ownership, and idempotency.
- Do not connect the mobile app to these routes until API contract and security tests pass.

## Implementation sequence (not yet authorized)

1. Finish a .NET 10 compatibility inventory and record upgrade sequencing/risks.
2. Close the remaining decisions above and update this gate.
3. Obtain explicit owner approval to begin implementation.
4. Build and test the API host/health slice.
5. Build and test Identity + OpenIddict configuration and customer mapping.
6. Implement customer commerce-order endpoints and security/idempotency tests.
7. Connect the mobile app only after the server contract is verified; test Android runtime and Arabic RTL / English LTR.
8. Keep the integration PR Draft until verification is complete and obtain separate owner approval before merging.

## Exit criteria

- Owner decisions and remaining open items are recorded accurately.
- .NET 10 dependency compatibility and upgrade sequence are documented before target-framework changes.
- API host configuration and security boundaries are tested.
- The authenticated subject maps deterministically to an internal customer ID.
- No client-supplied identity is trusted.
- Cross-customer reads are denied and concealed consistently.
- Legacy unowned orders remain inaccessible to customers.
- API contract, security, persistence, idempotency, and mobile integration tests pass before the PR is promoted out of Draft.

## Explicit non-goals for this gate

- No code implementation or package changes.
- No project/solution target-framework changes yet.
- No database migration or schema changes for Identity/OpenIddict yet.
- No API endpoint exposure beyond the future health/readiness slice.
- No PR merge or promotion out of Draft.
