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

## Initial .NET 10 compatibility inventory — research only

This section records repository facts and upstream compatibility guidance. It is **not** evidence that an upgrade has been built or tested.

### Repository facts observed

- Domain, Application, Infrastructure and test projects currently target `net8.0`.
- Infrastructure references `Microsoft.EntityFrameworkCore 8.0.31` and `Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11`.
- Infrastructure tests reference the same EF Core and Npgsql provider versions; test packages currently include `Microsoft.NET.Test.Sdk 17.11.1`, `xunit 2.9.2`, `xunit.runner.visualstudio 2.8.2`, and `coverlet.collector 6.0.2`.
- `.github/workflows/dotnet.yml` installs `.NET 8.0.x`, restores `LocalCommercePlatform.sln`, runs the solution tests, and exercises PostgreSQL 16 migration and backup/restore behavior.
- No `LocalCommerce.Api` project or Identity/OpenIddict package configuration is present in the reviewed baseline.

### Upstream compatibility findings

- .NET 10 is an active LTS release, with support scheduled through November 2028. The exact SDK patch should be selected from the current supported patch line: https://dotnet.microsoft.com/en-us/platform/support/policy
- EF Core 10 targets .NET 10 and is an LTS release supported through November 10, 2028. EF Core 10 requires the .NET 10 SDK/runtime: https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0 has been released with EF Core 10 support. The exact stable patch and dependency graph must be verified at restore time: https://www.npgsql.org/efcore/release-notes/10.0.html
- OpenIddict documents support for ASP.NET Core 10.0 / .NET 10. Its current major branch is 7.x according to its support policy; select a compatible stable patch and verify package dependencies before adoption: https://documentation.openiddict.com/integrations/aspnet-core and https://openiddict.com/support
- EF Core major upgrades can include breaking changes. Review the EF Core 9 and EF Core 10 breaking-change guides against this codebase before upgrading from EF Core 8: https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/breaking-changes and https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes

### Recommended upgrade sequence — not executed

1. Inventory every project file, package reference, SDK/tool requirement, GitHub Actions workflow, and test/runtime dependency in the solution.
2. Review EF Core 9 and 10 breaking changes against current persistence code and tests; record any required code changes before implementation.
3. In a dedicated upgrade change, align project target frameworks, EF Core packages, the Npgsql provider, and CI SDK to compatible stable .NET 10 / EF Core 10 / Npgsql 10 versions. Add Identity/OpenIddict packages only in the later API/auth implementation stage, after their detailed security profile is approved.
4. Restore and build the entire solution with the .NET 10 SDK; run all tests and the existing PostgreSQL 16 migration and backup/restore smoke test. Verify fresh database initialization and upgrade from the legacy schema.
5. Record actual command results and any remaining compatibility gaps. Do not call the upgrade complete based on package metadata alone.

**Current result:** compatibility direction looks feasible from upstream support information, but the repository has not yet been upgraded or tested on .NET 10. The target-framework and package changes remain unauthorized until the implementation gate is separately approved.

## Customer identity mapping — code review findings (proposal, not approved)

### Verified from inspected files

- The solution file currently lists Domain, Domain.Tests, Application, Application.Tests, Infrastructure, and Infrastructure.Tests; it does not yet include an API host or a dedicated customer-domain project.
- The inspected domain `Order` aggregate has order/store/items/status behavior but no customer identity property.
- The persisted Infrastructure `OrderEntity` separately contains nullable `Guid? CustomerId`; EF Core indexes this field. Therefore customer ownership currently exists as a persistence-level attribute, not as a demonstrated Customer aggregate relationship in the inspected domain model.
- The order-creation command accepts `Guid CustomerId`, while the handler checks the cart's CustomerId matches it. The HTTP boundary must therefore populate this value from the authenticated server-side identity mapping, never directly from request JSON.
- The repository's existing order schema intentionally allows null CustomerId for legacy rows. Do not backfill or expose those rows without independently verified ownership evidence.

### Recommended mapping proposal for owner review

1. Treat ASP.NET Core Identity's user ID as an authentication/account identifier, not as the commerce CustomerId.
2. Add a dedicated persistence mapping between the Identity user and the internal commerce customer identity. Prefer an explicit customer/account link with a unique Identity user key and a unique internal CustomerId where the product rule is one login account per customer. If shared/family accounts or multiple login methods per customer are a requirement, that cardinality must be decided before enforcing both uniqueness constraints.
3. Resolve the authenticated `sub`/Identity user ID to the internal CustomerId server-side on each authenticated request (or through a carefully designed trusted principal transformation). If no active mapping exists, fail closed; never fall back to a client-provided ID.
4. Define provisioning explicitly: registration creates the authentication account; customer mapping is created atomically by a trusted server workflow, with duplicate/conflicting mapping rejected. Do not auto-claim existing unowned orders during registration.
5. Account disablement blocks authentication/access immediately. Account deletion and customer-data retention need a separate policy; do not cascade-delete historical orders or financial/operational records merely because an Identity account is deleted.
6. Keep the mapping unique and auditable, and test missing, duplicate, disabled, deleted, and cross-customer mapping scenarios.

### Owner decisions recorded after review

- **Cardinality:** one login account per customer. Do not support multiple login accounts linked to the same customer in the first version. Enforce uniqueness in the eventual mapping only after the detailed schema design is reviewed.
- **Separate provisioning step:** creating an Identity login account and establishing the commerce customer record/link are distinct business steps. The customer must be identified and the intended customer profile confirmed by a trusted server-side workflow before the mapping becomes active. Do not infer customer ownership solely from a phone number, email address, or client-supplied CustomerId; define the required verification and duplicate-resolution rules in the provisioning design.
- **Customer definition:** for this API slice, a *customer* is the individual/end-user who buys goods or services through Baladna and owns their customer-facing orders. A *login account* is the credential/security principal used to authenticate that person. A *customer profile* is the platform's commerce-side record for that person and their customer preferences/contact/address data as explicitly modeled. These concepts must not be conflated.
- **Commerce records definition:** *commerce records* are transactional/operational records created by platform activity, such as orders, order items, status/history, and—when implemented—payment/refund records. They are not the same as authentication credentials or editable profile data. Existing orders with null/unverified CustomerId must not be automatically claimed or exposed.
- **Account lifecycle:** disable/suspend the login account rather than automatically deleting the customer and their historical commerce records. A disabled account must lose access immediately. Retention, privacy erasure, and any required anonymization of personal data must be handled by a separately defined policy; disablement is not itself a deletion request.
- **Implementation boundary:** these decisions clarify the design only. They do not authorize implementation, migrations, package/framework changes, or PR promotion/merge.

**Decision status:** the above product directions are owner-approved. The exact customer verification method, data fields, disablement enforcement mechanism, and retention/anonymization policy still require detailed design. No domain model, schema, migration, package, or runtime code was changed by this review.

## Native mobile OAuth/OIDC security profile — recommendation, not implementation approval

### Repository evidence

- `apps/customer-mobile/package.json` identifies an Expo Router / React Native app using Expo SDK `~57.0.16` and React Native `0.86.3`.
- `apps/customer-mobile/app.json` declares the `baladna` URL scheme and the Android/iOS application identifiers.
- The inspected mobile dependencies do not currently include an OAuth/OIDC client library or a secure token-storage package. The app authentication UI is not evidence of a working authentication protocol.
- No API host or OpenIddict configuration exists in the reviewed backend baseline.

### Recommended profile for owner review

1. **Native authorization flow:** use OpenID Connect Authorization Code Flow with PKCE (S256) for the native public client. Do not use the implicit flow or resource-owner-password grant. Do not ship a client secret in the mobile bundle.
2. **Redirect handling:** register exact redirect URIs for the app, prefer platform-appropriate claimed HTTPS redirects where operationally supported, and treat the existing `baladna` scheme as a candidate that still requires collision and platform-configuration review. Validate redirect URIs exactly; do not allow arbitrary wildcard redirects.
3. **Token validation:** the API validates signature, trusted issuer, intended audience, expiration/not-before, and required scopes/claims. A mobile UI check is never authorization. Customer ownership still comes from the server-controlled account-to-customer mapping.
4. **Access-token lifetime:** use short-lived access tokens with a documented policy; select the actual lifetime based on the API threat model and operational requirements before configuration.
5. **Refresh tokens:** if persistent sessions are required, use refresh-token rotation and replay detection/revocation, bind token state to the account/session as supported, revoke on account disablement and relevant security events, and test concurrent refresh behavior. Do not silently reuse a rotated refresh token.
6. **Secure storage:** keep tokens out of AsyncStorage, logs, crash reports, analytics, URLs, and deep-link payloads. Use a maintained platform-backed secure-storage mechanism after verifying compatibility with the selected Expo SDK and Android/iOS runtime. Keep any transient authorization code handling within the OIDC library's supported flow.
7. **Signing keys and issuer:** production must use stable HTTPS issuer URLs and protected signing keys/certificates with documented rotation and recovery. Do not rely on ephemeral development signing keys for production or commit secrets to the repository.
8. **Logout and account disablement:** define local session clearing and server-side refresh-token/session revocation separately. Account disablement must block future authorization promptly; do not assume deleting local tokens alone revokes server-issued credentials.
9. **Error handling and observability:** never log passwords, authorization codes, access/refresh tokens, or secrets. Record security-relevant events with correlation IDs and minimal personal data. Return stable API errors without exposing protocol internals.
10. **Verification before mobile integration:** test invalid issuer/audience/signature, expired tokens, PKCE mismatch, invalid redirect URI, refresh-token replay, revoked/disabled accounts, missing customer mapping, and cross-customer order access on the API. Test Android deep links and release builds; development-client success alone is insufficient.

### Decision boundary

This section is a security recommendation, not an approved implementation specification. Exact OpenIddict endpoints, client registration, redirect URIs per environment, scopes, token lifetimes, signing-key provider, refresh-token settings, secure-storage package, and logout semantics remain to be verified and approved. No mobile dependencies, app configuration, API code, secrets, or runtime settings were changed.

## Identity persistence, migrations, recovery and abuse controls — review proposal

### Persistence and migration ownership

Observed in the current infrastructure project:
- The backend targets `net8.0` and references EF Core 8.0.31 with Npgsql EF provider 8.0.11.
- The existing infrastructure embeds SQL files under `Persistence/Migrations/*.sql`; the CI workflow runs the solution tests and a PostgreSQL 16 backup/restore smoke test.
- No API host, Identity store, OpenIddict store, or identity-specific migration is present in the reviewed baseline.

Recommended design, pending approval:
1. Keep identity/account persistence in the existing PostgreSQL deployment initially unless scale, security isolation, or operations requirements justify a separate database. This is a deployment recommendation, not a claim that the schema has been implemented.
2. Decide explicitly whether identity tables live in a dedicated PostgreSQL schema and how Identity/OpenIddict tables relate to the existing commerce schema. Avoid accidental coupling through shared entity types or cascade deletion into orders.
3. Choose one authoritative migration process for each schema. The existing embedded SQL migration runner and EF Core migrations must not both attempt to own the same tables. Document migration ordering, fresh-database setup, upgrades from existing installations, rollback/forward-repair strategy, and deployment locking before adding identity tables.
4. Do not run schema migrations automatically at application startup in production without an approved deployment and concurrency strategy. Apply migrations through a controlled deployment step with backup/recovery expectations.
5. Verify restore from backup and the existing migration/recovery workflow after any eventual schema change; tests must use PostgreSQL rather than relying only on EF InMemory behavior.

### Registration, recovery and abuse protection

Recommended minimum policy to design and test:
1. Define the login identifier first (verified phone, verified email, or another explicitly approved option). Do not assume an unverified phone/email uniquely proves a person's identity or customer ownership.
2. Keep account creation separate from customer-profile provisioning. The profile-to-account link becomes active only after the chosen verification and duplicate-resolution workflow succeeds.
3. Provide account recovery through a verified channel, with single-use, expiring recovery tokens; do not disclose whether an account exists in public recovery responses.
4. Configure password policy and breached-password defenses according to the selected Identity capabilities and product risk. Avoid arbitrary complexity rules that encourage weak predictable passwords.
5. Add rate limits and progressive abuse controls to registration, login, verification resend, and password reset. Use account lockout carefully so an attacker cannot trivially lock out another user; combine per-IP/device and per-account controls where feasible.
6. Set explicit expiry and resend cooldowns for verification challenges; prevent replay and avoid putting codes or tokens in URLs, logs, analytics, or support messages.
7. Audit security events with correlation IDs and minimal personal data. Do not log credentials, one-time codes, access tokens, refresh tokens, or recovery tokens.
8. Keep email/SMS provider selection, delivery costs, anti-abuse thresholds, and service-level expectations open until requirements and operational cost are compared.

### Verification checklist before implementation approval

- Identity schema and OpenIddict schema can be created on a fresh PostgreSQL database and upgraded safely from the existing schema.
- Re-running migration/deployment steps is safe and does not duplicate or corrupt schema state.
- Duplicate account registration and conflicting customer mappings fail safely.
- Unverified contact methods cannot activate a customer mapping.
- Recovery responses do not reveal account existence; recovery tokens expire, are single-use, and cannot be replayed.
- Rate limits, lockout behavior, resend cooldowns, and audit events are tested without leaking secrets.
- Disabling an account prevents new authentication and refresh operations according to the approved revocation model.
- Existing orders and customer data are not cascade-deleted when an Identity account is disabled or removed.

**Decision boundary:** these are review recommendations only. No database/schema, migration, account policy, delivery provider, package, API, or runtime configuration was changed. Exact contact verification, recovery channel, lockout/rate-limit thresholds, schema ownership, and migration deployment strategy remain open for owner review.

## Decisions still open before implementation

1. **Provisioning verification details:** specify how a person is verified and matched to the customer profile in the separate provisioning step, how duplicate profiles/conflicts are resolved, and how mapping activation is made atomic and auditable. Always fail closed if the mapping is missing or inactive.
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

1. Finish the .NET 10 compatibility inventory and record upgrade sequencing/risks.
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


## Customer Orders API contract — repository review proposal

**Review status: PARTIAL; no endpoint implementation or runtime test was performed.** This section separates verified application/persistence behavior from proposed HTTP behavior. The reviewed branch still has no ASP.NET Core API host, so route names, HTTP status mapping, authentication middleware, and wire DTOs are not implemented or verified.

### Verified existing order-creation behavior

Reviewed files:
- `src/LocalCommerce.Application/Ordering/CreateOrder/CreateOrderApplication.cs`
- `src/LocalCommerce.Domain/Ordering/Order.cs`
- `src/LocalCommerce.Infrastructure/Ordering/CreateOrderPersistence.cs`
- `src/LocalCommerce.Infrastructure/Persistence/CommerceDbContext.cs`
- `src/LocalCommerce.Infrastructure/Persistence/Migrations/002_order_customer_ownership.sql`

Findings from those files:
1. The application command is `CreateOrderCommand(CustomerId, CartId, IdempotencyKey)`. CustomerId is an application input today, not authenticated identity. The future HTTP adapter must derive it from the validated principal and active server-side customer mapping; it must never trust a client-supplied CustomerId.
2. The handler checks that the cart belongs to the supplied customer, is active and non-empty, validates positive quantities, validates active store/orderable products and variant consistency, and calculates item prices from current product snapshots rather than trusting client prices.
3. The handler creates an order and consumes the cart inside the same unit-of-work transaction, and stores an idempotency completion result. The database model has a unique idempotency index on `(ScopeId, Operation, IdempotencyKey)`; order numbers are unique.
4. The request fingerprint currently includes CustomerId and CartId. The idempotency lookup/reservation is scoped using customer ID and operation. A pre-read exists before the transaction, with a second reservation attempt inside the transaction.
5. Orders have a nullable CustomerId for legacy compatibility. Migration `002_order_customer_ownership.sql` adds that nullable column and an index; it does not establish ownership for legacy rows.
6. The current domain Order object contains order ID, store ID, items, and status, but not customer ownership. Customer ownership is stored by the infrastructure OrderEntity. This makes repository/application authorization boundaries especially important.

### Proposed minimal customer-facing contract — pending owner approval

Keep this first commerce API slice limited to customer order operations. Suggested route shape (not implemented):
- `GET /api/v1/customer/orders` — only the authenticated customer's orders, paginated.
- `GET /api/v1/customer/orders/{orderId}` — one order only if it is owned by the resolved customer.
- `POST /api/v1/customer/orders` — create from an owned cart, with an `Idempotency-Key` request header.

Do not add catalog, store-management, driver, admin, payment, refund, or general commerce endpoints to this slice.

### Read access and response data

1. Resolve customer identity once per request from the validated subject-to-customer mapping. Missing, disabled, or ambiguous mappings fail closed.
2. Apply ownership filtering in the database query itself (resolved CustomerId AND requested order ID), not after loading an arbitrary order and not by role-only checks.
3. For order detail, return the same not-found response for nonexistent and other-customer orders (404) to avoid leaking order existence. A missing authentication credential should be 401; an authenticated but unmapped/disabled customer should follow the explicitly approved authorization policy without exposing profile details.
4. List endpoint must have stable ordering (e.g. CreatedAt descending plus ID tie-breaker), bounded page size, and an opaque/validated continuation strategy if cursor pagination is chosen. Do not return all historical orders by default.
5. Expose only customer-required fields: public order identifier/number, created/updated timestamps, status, item snapshots (name/variant/quantity/unit price/line total), and any approved totals. Do not expose internal CustomerId, identity subject, other customers' details, internal failure diagnostics, or unrestricted persistence entities.
6. Decide whether store details, delivery status, addresses, discounts, taxes, and totals are available in this slice only after checking existing data sources and privacy requirements. Do not fabricate fields not present in persisted data.
7. Treat legacy orders with null CustomerId as invisible. No automatic claim by matching phone, email, address, or a client-supplied order number; any backfill needs verified evidence and an auditable process.

### Create-order transport and validation

1. Request body should identify the owned cart only; it must not accept CustomerId, prices, totals, order status, store ownership, or authoritative product snapshots from the client.
2. Require an idempotency key with explicit length/character limits and a documented retention window. Avoid placing keys in URLs or logs.
3. Return a stable created-order representation. A replay with the same customer, operation, key, and equivalent request should return the recorded result without creating another order or consuming the cart twice.
4. Reusing the same key for a different request must return a stable conflict/error contract. Do not expose raw database uniqueness exceptions.
5. A concurrent first-use race must be handled deliberately: the unique database index may reject one reservation attempt. After a conflict, re-read the idempotency record and validate its fingerprint/result; do not blindly retry order creation or convert every uniqueness violation into success. Confirm the exact transaction/isolation behavior with PostgreSQL integration tests.
6. Confirm that the idempotency reservation, order write, cart consumption, and completion record commit or roll back atomically. Test injected failures before/after each persistence step and process interruption/retry scenarios.
7. Decide stable HTTP status and RFC 9457 Problem Details mappings for invalid cart, unavailable product/store, empty cart, invalid/reused idempotency key, and unexpected failure. Keep application error codes stable and never return stack traces or SQL details.

### Required verification before this contract can pass

- Unauthenticated list/detail/create requests are rejected.
- Customer A cannot list or retrieve Customer B's orders, even when IDs are guessed; detail responses conceal existence consistently.
- Client-supplied CustomerId fields/headers/URLs cannot change the resolved customer.
- Missing, inactive, duplicated, or unverified customer mappings fail closed.
- Null-CustomerId legacy orders are not visible.
- Pagination is bounded and stable; invalid cursors/page sizes fail safely.
- Create-order price/quantity/store/cart tampering is rejected or ignored according to the approved DTO contract.
- Repeated same-key requests create exactly one order and consume the cart at most once; a different request with the same key is rejected.
- Concurrent duplicate requests and injected failures preserve atomicity and yield a deterministic replay result.
- Database constraint conflicts are mapped to safe, stable API errors.
- Tests run against PostgreSQL, alongside existing migration and backup/restore checks; unit tests alone are insufficient.

### Review gaps and decisions still open

- The reviewed files confirm the create-order application handler and infrastructure persistence, but I did not verify a complete existing customer-order list/detail query contract or a running HTTP authorization adapter on this branch. Treat those as unverified, not as proof that no related code exists elsewhere in the repository.
- Approve the route shape, response DTO fields, pagination strategy, idempotency-key policy/retention, exact HTTP/error mapping, and behavior for inactive/unmapped customer accounts.
- The handler's concurrent idempotency reservation behavior and PostgreSQL failure/retry cases require targeted integration tests before implementation approval.

**Decision boundary:** this is documentation-only design review. No application code, API host, DTO, endpoint, database schema, migration, package, CI configuration, or PR state was changed by this review.


## Create-order idempotency and failure-path review — partial findings

**Status: application flow, concrete persistence-store implementation, and existing PostgreSQL test coverage reviewed from source. No tests were executed in this review session; CI evidence must be checked separately before claiming current green status.**

### Confirmed from the current application handler and EF model

1. `CreateOrderHandler` first reads an idempotency record using the customer scope, operation `CreateOrder`, and the supplied key. If a record exists, it validates the request fingerprint and returns the stored completed result; a persisted `Reserved` record is treated as an unexpected failure because the handler cannot prove that work is still in progress.
2. If no record is found, the handler validates the cart/store/products and builds the order before opening its unit-of-work transaction. Inside the transaction it attempts to reserve the key again, writes the order, consumes the cart, and marks the idempotency record complete.
3. The fingerprint is SHA-256 over normalized GUID text for CustomerId and CartId. Thus it identifies the cart-based command, not a snapshot of the cart's line contents or prices. That may be correct if the contract defines the idempotent command as “checkout this cart once,” but it must be an explicit decision.
4. EF model configuration declares a unique database index on `(ScopeId, Operation, IdempotencyKey)`. This is useful protection against duplicate reservations, but it does not by itself prove that a concurrent request is translated into a deterministic replay response.
5. The unit-of-work implementation begins a database transaction, runs the operation, saves changes, and commits; on an exception it attempts rollback and rethrows. The exact behavior still depends on the concrete idempotency-store implementation, EF tracking state, PostgreSQL uniqueness handling, and how a failed transaction is recovered.

### Risks to close before calling idempotency reliable

- **Concurrent reservation race:** both requests can observe no existing record before either reserves it. One transaction may hit the unique constraint. The implementation must distinguish that specific reservation conflict from unrelated database failures, recover/clear failed EF state as needed, then re-read the winner's record in a fresh valid transaction/context and validate its fingerprint/result. Do not assume a failed PostgreSQL transaction can continue normally.
- **Result visibility and transaction boundaries:** order creation, cart consumption, and the completed idempotency result should commit atomically. If reservation is committed independently, a crash can strand a permanent `Reserved` record; if it is inside the same transaction, an interrupted transaction should roll back, but this needs verification against the actual store implementation.
- **Replay semantics after cart changes:** the handler computes the fingerprint from customer and cart IDs, while it reads cart contents before the reservation inside the transaction. Define whether a retry after a cart changes should replay the original result, reject the changed command, or treat the cart as a distinct checkout intent. Current fingerprinting does not detect changed cart contents.
- **Key validation and normalization:** define accepted length/characters, whitespace handling, maximum retention, and whether key comparison is case-sensitive. Ensure the database key limit and HTTP contract agree. Do not log raw keys.
- **Cancellation and transient failures:** verify cancellation at each awaited step, connection loss around commit, retry after an ambiguous commit result, and rollback failure behavior. A client timeout does not prove that the order transaction failed.
- **Order-number generation:** generation currently occurs before the transactional operation. Confirm uniqueness-conflict handling and ensure retries do not accidentally create multiple orders or return an inconsistent result.

### Required PostgreSQL integration test matrix

1. Same customer + same key + same cart sent sequentially: one order, cart consumed once, stable result replayed.
2. Same customer + same key + different cart: deterministic key-reuse error and no second order.
3. Same key concurrently submitted from two independent database contexts: exactly one order, one cart consumption, and both callers receive a consistent result or one receives a documented retryable response.
4. Inject failure before order insert, after order insert but before cart consumption, after cart consumption but before completion, and around commit: all persisted state must be atomic and safe to retry.
5. Simulate ambiguous commit/connection loss and then retry with the same key; verify no duplicate order.
6. Force the unique idempotency index conflict and verify that only the expected constraint is handled as a reservation race; unrelated uniqueness/database errors must remain failures.
7. Reuse the same key with changed cart contents and verify the explicitly chosen semantic.
8. Verify key length/normalization, completed record corruption, stale reservation handling, and result payload/resource-ID consistency.
9. Verify transaction rollback and EF DbContext state recovery after a PostgreSQL constraint violation.
10. Verify idempotency scope includes authenticated customer and operation so another customer or another operation cannot replay the record.

### Recommendation and decision boundary

Keep the existing database unique index as a necessary invariant, but do **not** mark create-order idempotency as fully verified until the concrete store implementation and these PostgreSQL concurrency/failure tests are inspected and pass. Decide explicitly whether the fingerprint means “same cart checkout intent” or “same immutable request payload.” This is a documentation-only review: no code, schema, migration, package, runtime setting, test result, or PR state was changed.


## Create-order idempotency — concrete store and test review

**Scope:** source inspection of `GeneralizedIdempotencyPersistence.cs`, `CreateOrderPersistence.cs`, `GeneralizedIdempotencyPersistenceTests.cs`, and `CreateOrderPersistenceTests.cs`. This review did not execute tests or change runtime code.

### Confirmed implementation behavior

1. `EfGeneralizedIdempotencyStore.ReserveAsync` uses a parameterized PostgreSQL `INSERT ... ON CONFLICT ("ScopeId","Operation","IdempotencyKey") DO NOTHING`. If it inserts no row, it re-reads the existing record and returns it. This is stronger than relying on a raw unique-constraint exception and is designed to handle the normal unique-key race without leaving the PostgreSQL transaction aborted.
2. The uniqueness scope is `(ScopeId, Operation, IdempotencyKey)`. The store itself returns the existing record even when the supplied fingerprint differs; the command handler is responsible for comparing fingerprints and rejecting key reuse.
3. `CompleteAsync` loads the reservation, rejects missing or already-completed records, and sets the completed resource reference/payload. The create-order unit of work wraps reservation, order write, cart consumption, completion, `SaveChangesAsync`, and commit in one transaction.
4. Because reservation and completion are inside the same create-order transaction, the intended behavior is that a failure before commit rolls both back. The handler treats a previously persisted `Reserved` record as an unexpected failure; this is appropriate for detecting an invariant violation but does not itself recover a stale reservation created by another caller or code path.

### Existing PostgreSQL tests located

- `GeneralizedIdempotencyPersistenceTests.Concurrent_reservation_allows_only_one_winner`: uses two independent DbContexts and transactions, holds the first insert uncommitted, then verifies the second reservation waits and returns the first record after commit.
- `GeneralizedIdempotencyPersistenceTests.Reservation_and_completion_rollback_as_one_transaction`: verifies reservation and completion disappear together after rollback.
- `CreateOrderPersistenceTests.Same_idempotency_key_converges_to_one_order`: concurrent same-key requests using independent DbContexts return the same Order ID and leave one order.
- `CreateOrderPersistenceTests.Concurrent_different_keys_on_same_cart_create_only_one_order`: verifies only one of two different-key attempts can create an order from the same cart.
- `CreateOrderPersistenceTests.Failure_after_order_write_rolls_back_order_and_cart`: injects a failure during cart checkout after an order write and checks that the order and idempotency record are absent and the cart remains active.
- Other tests cover replay fingerprint conflicts, completed payload persistence, and cart-based key reuse.

### Remaining gaps / verification caveats

1. **No test run was performed in this review.** The presence of test code is not proof that the current branch's tests pass. Inspect the latest workflow run for the current head before using a GREEN/VERIFIED label.
2. The concurrent reservation test uses a fixed `Task.Delay(100)` to assert the second operation is waiting. This can be scheduler-sensitive; a bounded synchronization/timeout approach would make the test more deterministic.
3. Existing tests do not establish behavior for a connection loss or ambiguous result around transaction commit, nor exhaustive failure injection at every boundary (before insert, after cart consumption, during completion, and during commit).
4. The rollback test that deliberately calls `SaveChangesAsync` inside a transaction before throwing verifies persisted rollback state. It does not fully establish that reusing the same DbContext after rollback is safe in every EF change-tracker state; a targeted same-context retry test or a documented fresh-context retry boundary is still needed.
5. The command fingerprint remains based on CustomerId + CartId, not cart-line/pricing content. This is consistent only if the product contract deliberately defines the idempotent intent as “checkout this cart once.” The owner-facing API contract must decide whether cart mutations after the first attempt should replay the first order or be rejected.
6. Key normalization, whitespace-only/oversized keys, retention/expiry, stale reservation operational handling, malformed persisted result payloads, and explicit HTTP mapping of database/transient errors remain outside the verified coverage found here.

### Updated assessment

The concrete store has a sound PostgreSQL conflict-resolution pattern for ordinary concurrent reservations, and the repository contains meaningful integration tests for reservation contention, same-key replay, cart contention, and transaction rollback. The remaining blocker is **verification breadth and current CI evidence**, not a proven defect in the `ON CONFLICT` implementation itself. Do not claim all ambiguous-commit, stale-reservation, DbContext-recovery, or HTTP error-mapping paths are covered until they are tested and the relevant CI run is checked.

**Decision boundary:** this remains a documentation-only review. No code, schema, migration, package, runtime setting, test, or PR state was changed.
