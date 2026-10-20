# Customer Authentication and First API Slice — Design Gate

## Latest verification status — 2026-10-20

This is the current verification record for the document. Older status notes below are historical and must not override this section.

- **Latest fully confirmed CI evidence for the staged sign-in/biometrics decision:** commit `6ff09aaa440f04c0b4325c70d67e61456ae711a5` passed all four configured GitHub Actions runs on October 20, 2026: [.NET PR workflow](https://github.com/palestiny/local-commerce-platform/actions/runs/39456972099), [customer-mobile PR workflow](https://github.com/palestiny/local-commerce-platform/actions/runs/39456972072), [.NET push workflow](https://github.com/palestiny/local-commerce-platform/actions/runs/39456960199), and [customer-mobile push workflow](https://github.com/palestiny/local-commerce-platform/actions/runs/39456960302). This is the last fully verified commit, not a claim that every later documentation-only commit has passed.
- **Current-head rule:** check the GitHub Actions runs for the actual current branch head before reporting its CI status. A documentation-only update can trigger new runs; until they complete, mark current-head CI as pending. Passing these workflows does not establish biometric behavior on physical devices, live mobile-to-API authentication, or authorize authentication implementation.
- The configured .NET workflow builds/tests the solution and runs its PostgreSQL 16 migration/backup-restore smoke check; the mobile workflow runs TypeScript checking and Expo Doctor. These results do **not** establish Android/iOS runtime behavior, live mobile-to-API integration, a physical PostgreSQL connection failure during COMMIT, or every failure-injection scenario.
- PR #22 remains open and Draft. No API/auth implementation, framework/package change, database change, or PR promotion/merge is authorized by this documentation update.

### Support-window implication

- Microsoft's current release information lists .NET 10 SDK `10.0.402` / runtime `10.0.13` (released October 13, 2026), and .NET 8 servicing release `8.0.32` for the same October servicing cycle: https://dotnet.microsoft.com/en-us/download/dotnet/10.0 and https://devblogs.microsoft.com/dotnet/dotnet-and-dotnet-framework-october-2026-servicing-updates/
- .NET 8 support ends in November 2026; .NET 10 LTS support runs through November 2028: https://learn.microsoft.com/en-us/dotnet/core/releases-and-support
- This makes the .NET 10 upgrade time-sensitive, but does not justify skipping compatibility review. Before upgrading, verify current stable patch versions for every EF Core/Npgsql package and the full restore graph, review breaking changes, then build/test and run PostgreSQL migration/recovery checks on the dedicated upgrade change.



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
- The repository has three production projects (Domain, Application, Infrastructure) and three corresponding test projects; all six target `net8.0`. The reviewed recursive tree contains no `global.json`, `Directory.Build.props`, `Directory.Packages.props`, or shared targets file, so framework/package versions are declared in individual project files.
- The infrastructure and infrastructure-test projects currently use EF Core `8.0.31` and Npgsql EF provider `8.0.11`; the infrastructure test project also uses Test SDK `17.11.1`, xUnit `2.9.2`, xUnit runner `2.8.2`, and coverlet `6.0.2`. These are observed declarations, not a restore-time compatibility proof for .NET 10.
- The official .NET download page lists SDK `10.0.402` and runtime `10.0.13` as the latest .NET 10 patch line on 2026-10-20; Microsoft's October servicing note lists .NET 8 `8.0.32`: https://dotnet.microsoft.com/en-us/download/dotnet/10.0 and https://devblogs.microsoft.com/dotnet/dotnet-and-dotnet-framework-october-2026-servicing-updates/. Use the latest supported patch at actual upgrade time; do not hard-code this snapshot into long-lived policy.
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

## Create-order idempotency contract — reviewed and test-backed

This contract describes the current implementation boundary; it does not authorize changes to the HTTP API or database schema.

- **Key namespace:** a key is unique within the pair `(customer scope, operation)`. For checkout the operation is `CreateOrder`; two different customers do not collide, and different operations have separate namespaces.
- **Exact key handling:** a key must contain a non-whitespace character and must be at most 256 .NET string characters. The key is otherwise treated as opaque and compared exactly; it is not trimmed or case-folded. Clients should generate a fresh, unpredictable key per checkout attempt and reuse that exact key for retries of that attempt.
- **Request binding:** the current CreateOrder fingerprint binds the authenticated/internal customer ID and cart ID. Reusing the same key for a different cart is rejected as `IdempotencyKeyReused`. The cart's line items and prices are not part of the fingerprint: the key identifies one checkout attempt for that customer/cart, not a mutable serialization of the cart.
- **Successful replay:** once checkout succeeds, retrying the same customer + operation + key + cart returns the stored original order ID and order number, even though the cart has already been consumed. It must not create a second order or consume the cart again.
- **Atomicity:** reservation, order persistence, cart consumption, and completion of the idempotency record must commit or roll back in the same database transaction. A failed transaction must leave no completed reservation and must not leave a partial order/cart mutation.
- **Concurrency:** competing requests with the same key converge on one persisted result. Different keys against the same cart may not create duplicate orders; only one may successfully consume the active cart.
- **Unresolved reliability boundary:** if the client loses its connection around commit, the client must retry with the same key. The server should return the committed result if commit succeeded, or safely allow a new attempt if the transaction rolled back. Automated tests currently cover transaction rollback/retry and successful replay, but do not yet simulate a genuinely ambiguous commit/connection loss.
- **Scope limitation:** this is a checkout idempotency contract, not a payment idempotency contract. Payment authorization/capture/refund must have separate provider-operation keys and reconciliation rules when payment is introduced.

### Evidence and remaining verification

The infrastructure suite covers key length at and over the storage limit, same-key concurrent convergence, sequential replay (added with this update), different-cart key reuse rejection, different-key contention on one cart, and rollback after a failure following order persistence. The generalized store also has a PostgreSQL lock-wait concurrency test and a transaction rollback/retry test. Passing CI is required on the current commit before this point is marked verified.

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

## Unified account, customer profile, merchant business and disablement — owner-approved product rules

These product rules clarify identity concepts and account lifecycle. They are not a database schema or implementation authorization.

1. **One login account per person for the first version.** ASP.NET Core Identity owns the login/security principal. The account may be eligible for more than one platform role (for example CUSTOMER and MERCHANT); do not create a second login merely because the person shops and also operates a business. Roles describe broad capabilities, not ownership of a particular order, store or business.
2. **Customer means the buyer/person; customer profile means commerce data.** A customer is the individual who buys goods or services through Baladna and owns customer-facing orders. The customer profile is the platform's commerce-side record for preferences and explicitly modeled contact/address data. The Identity account is credentials and security state. They are related concepts, not the same record.
3. **Merchant is a business actor, not a Store.** A Merchant/business profile represents the trading business and may own multiple Stores. A person who operates a merchant business remains the same login account and may also be a customer. Merchant-owner/staff permissions must be scoped to the specific business/store relationship and verified server-side; a MERCHANT role alone does not grant access to every merchant or store.
4. **Commerce records are operational history.** Orders, order items, order status/history and future payment/refund records are transactional records. They are not authentication credentials and must not be cascade-deleted just because a login account or editable profile is disabled or removed. Legacy orders without verified ownership stay unavailable through customer APIs.
5. **Disable the login account = platform-wide login suspension.** A disabled/suspended Identity account must be prevented from obtaining new tokens and from refreshing existing sessions. Existing refresh tokens/sessions must be revoked or rejected promptly; API authorization must also enforce the approved revocation strategy rather than trusting an already-issued token indefinitely. Disablement is not account deletion, privacy erasure or an instruction to delete commerce history.
6. **Business/store suspension is separate from account suspension.** Disabling a merchant business or Store blocks the affected business operations according to its status policy, but does not automatically disable the person's whole login or erase their customer profile/orders. Conversely, disabling the person's login blocks their access across roles while preserving business and commerce records. The precise business-status transition rules belong to the merchant/store lifecycle gate.
7. **Provisioning remains a separate trusted workflow.** Creating a login account does not automatically create/claim a customer profile, merchant business, Store or historical order. Customer mapping and merchant membership become active only after the relevant verification, duplicate/conflict resolution and auditable server-side workflow succeeds. Contact verification alone is not proof of ownership of an existing commerce record.

### Acceptance tests implied by these rules

- One account can be both customer and an authorized merchant actor without duplicate login credentials.
- A customer can only list/read their own verified customer orders; merchant access is restricted to businesses/stores for which the account has an active authorized membership.
- A missing, disabled, duplicate or unverified mapping/membership fails closed.
- Disabling an account blocks login and refresh and invalidates/rejects existing sessions according to the selected token-validation/revocation mechanism.
- Suspending a merchant or Store blocks affected merchant operations without silently disabling unrelated customer capabilities.
- Disabling/removing an account or profile does not cascade-delete orders, order history or financial/operational records.
- Registration never auto-claims legacy orders or assigns merchant access based solely on client-supplied IDs, phone number or email address.

**Decision boundary:** the product semantics above are recorded as owner-approved directions from the prior review. Token revocation mechanics, identity/profile/business membership schema, verification channels, retention/anonymization rules and merchant/store status transitions still need detailed design. No runtime code or schema was changed by this clarification.


## Detailed registration, verification, recovery and abuse-control proposal — pending owner approval

This section advances the design but does not claim an implementation decision has been approved.

### Recommended product behavior

1. **Login identifiers:** support a verified email address or verified phone number as a login/recovery channel, subject to delivery-cost and operational review. Normalize identifiers consistently, enforce uniqueness only for verified identifiers, and do not treat either contact channel as proof that a person owns an existing customer profile, order, business or Store. A person may add a second verified channel later.
2. **Registration is not commerce provisioning:** registration creates a disabled/unverified login state or equivalent pending state. Successful contact verification activates the login according to policy, but does not automatically link a customer profile or create merchant access. A separate trusted provisioning workflow confirms the intended customer/business and records an auditable mapping/membership.
3. **Verification challenges:** use cryptographically unpredictable, single-use, purpose-bound, expiring challenges; store only protected/verifiable challenge material where feasible; enforce resend cooldowns and per-account/per-IP/device rate controls. Never put codes in URLs or logs. Responses should not reveal whether an identifier is already registered.
4. **Recovery:** provide recovery only through a previously verified channel. Public responses must be indistinguishable for existing and non-existing accounts. Challenges expire, are single-use, and cannot be replayed; successful recovery invalidates outstanding recovery challenges and follows the chosen session/refresh-token revocation policy. Do not silently change an account identifier based only on possession of an unverified new contact.
5. **Password and sign-in defenses:** use Identity's maintained password hashing and token primitives; set a documented password policy, breached-password protection where operationally available, and progressive rate limiting. Use lockout cautiously to avoid easy denial-of-service against a victim. MFA/passkeys can be a later security milestone unless risk assessment makes them mandatory.
6. **Delivery provider:** keep SMS/email vendor selection open until a small cost/reliability comparison is recorded. The application must depend on a delivery abstraction, not a vendor SDK in the Domain/Application core. Test provider failure, resend throttling and delivery observability without logging secrets.
7. **Audit:** record registration, verification success/failure, recovery, credential change, disable/enable, refresh-token replay/revocation and mapping changes with correlation IDs, timestamps and minimal personal data. Never log passwords, raw verification codes, access/refresh tokens, recovery tokens or full authorization headers.

### Decisions still requiring explicit owner approval

- **Approved: both email and phone are supported at first launch** as independently verifiable contact/login channels. A user may sign in with either verified identifier and their password; the product must not require both identifiers just to sign in.
- **Approved: activate after at least one channel is verified.** Successful verification of either email or phone permits account activation. The second channel may be added and verified later; its verification state remains independent. Requiring both channels before activation is explicitly not the chosen policy.
- The exact verification expiry, resend cooldown and rate-limit thresholds.
- SMS/email delivery provider and cost budget.
- Whether MFA/passkeys are in the first release or a later milestone.
- Retention, anonymization and privacy-erasure rules for identity/profile data versus legally/operationally retained commerce history.

### Acceptance tests

- Identifier normalization and verified-identifier uniqueness are deterministic.
- Registration/verification/recovery responses do not reveal whether an account exists.
- Expired, replayed, wrong-purpose and superseded challenges are rejected.
- Rate limits and resend cooldowns apply consistently; legitimate users are not permanently locked out by a single unauthenticated actor.
- A verified email/phone cannot claim an existing customer, order, business or Store without the separate approved provisioning proof.
- Disablement blocks login and refresh according to the selected revocation design; audit events contain no credentials or bearer tokens.

## Customer Orders API — recommended HTTP contract for owner review

The route shapes below remain proposals until the owner approves the wire contract and implementation is separately authorized.

### Routes and transport

- `GET /api/v1/customer/orders?limit={limit}&cursor={cursor}` — list the authenticated customer's orders.
- `GET /api/v1/customer/orders/{orderId}` — retrieve a single order owned by the authenticated customer.
- `POST /api/v1/customer/orders` — create an order from the authenticated customer's active cart; require the `Idempotency-Key` header.
- The POST body contains only the cart identifier required by the existing use case. It must not accept CustomerId, prices, totals, order status, store ownership, or authoritative product snapshots.
- Use cursor pagination with a bounded default and maximum limit. Order by creation timestamp descending plus a stable unique tie-breaker. Cursor contents must be opaque and validated; never accept arbitrary SQL/order expressions from clients.
- Detail/list DTOs should contain only fields actually persisted and approved for customers: public order identifier/number, timestamps, status, item snapshot fields, and approved totals. Do not expose internal CustomerId, Identity subject, persistence entities, diagnostics or another customer's data. Confirm actual persisted timestamp and total fields before finalizing the DTO.

### Recommended status and error mapping

| Situation | HTTP response | Rule |
|---|---:|---|
| Missing/invalid access token | 401 | Standard authentication challenge; no sensitive detail |
| Authenticated principal has no active customer mapping | 403 | Stable generic Problem Details code; do not return mapping internals |
| Order does not exist or belongs to another customer | 404 | Same response shape and code to conceal existence |
| Malformed JSON, invalid identifier/cursor, invalid limit | 400 | Stable validation errors; no stack traces |
| Cart is not available to this customer or cannot be checked out | 409 or 404 per approved cart-disclosure policy | Pick one mapping and keep it stable; do not reveal another customer's cart |
| Product/store/cart state makes checkout invalid | 409 | Stable application error code, not raw database details |
| Missing/invalid Idempotency-Key | 400 | Explain header requirement without echoing secrets |
| Same key reused for a different fingerprint | 409 | Stable code such as `idempotency_key_reused` |
| First successful create or successful replay | 201 | Return the same recorded public order representation and Location for the original order |
| Rate limit exceeded | 429 | Include Retry-After when known; never reveal account existence |
| Unexpected server/dependency failure | 500 or 503 as appropriate | RFC 9457 Problem Details, trace/correlation ID, no SQL/stack/token data |

All error responses should use RFC 9457 Problem Details with stable machine-readable codes. The table is a recommendation; exact application error names and cart-disclosure choices require review against existing typed application errors before coding. Do not map every database exception to a client error.

### Security and idempotency invariants

- Resolve the validated subject to one active customer mapping server-side. The client cannot select or override CustomerId.
- Put ownership predicates directly into repository/database queries for list and detail. Test cross-customer IDs and ensure list counts/cursors cannot reveal other customers' records.
- Treat a replay after the original order commits as the same logical result; return the original order identifier and representation. Same key plus a different cart/fingerprint is a conflict. Keep the current documented key bound of 256 .NET string characters, reject whitespace-only values, and compare the opaque key exactly.
- Define idempotency-record retention and operational monitoring before release. Do not expire records while clients may reasonably retry an uncertain checkout; a retention change needs an explicit duplicate-order risk assessment.
- Keep legacy orders with null/unverified CustomerId invisible. Any historical ownership backfill requires independent proof and an auditable process.
- API authorization, request validation, error mapping, and idempotency behavior must be verified with PostgreSQL-backed integration tests, not only unit tests.

### Required contract tests

- 401 without a valid access token; fail closed for missing, inactive, ambiguous or disabled customer mapping.
- Customer A cannot list or retrieve Customer B's orders even with a guessed identifier; not-found response is indistinguishable for nonexistent and not-owned IDs.
- Body fields, headers or query parameters cannot override the resolved CustomerId.
- Pagination is bounded, stable and isolated to one customer; invalid cursors fail safely.
- DTO contract contains no internal identity fields or unapproved persistence fields.
- Create-order validates cart ownership/state and derives prices from server-side product data.
- Same-key retry/replay returns one original order; conflicting key reuse is deterministic; concurrent requests and persistence failures do not duplicate orders or partially consume the cart.
- RFC 9457 responses contain stable codes and trace identifiers but no SQL, stack traces, secrets or bearer tokens.
- All API/auth integration tests run against PostgreSQL, with migration/recovery smoke tests retained.

**Decision boundary:** this is detailed design guidance, not authorization to add endpoints, packages, tables, migrations or mobile auth code. Before implementation, reconcile each recommendation with the current typed application errors and persisted OrderEntity fields, complete the .NET 10 dependency compatibility review, and obtain explicit approval for the remaining product choices.

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
4. **Account recovery and abuse controls:** the activation threshold is owner-approved: at least one of email or phone must be verified, and the second channel can be verified later. Still define password reset/recovery details, rate limiting/lockout, verification expiry/resend thresholds, and delivery provider. Provider and operational cost are not yet approved.
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


## Create-order idempotency — lost-response retry verification

**Latest verification addition:** PostgreSQL integration test `CreateOrderPersistenceTests.Retry_after_committed_order_but_lost_response_replays_original_result` commits the complete order transaction, deliberately simulates the caller losing the response after commit, then retries the same command with a fresh DbContext. The retry must return the persisted order, leave exactly one idempotency record, and leave the cart consumed exactly once.

This is a deterministic test of the client-visible ambiguous-outcome recovery contract **after a successful commit**. It is not a physical network fault injected during PostgreSQL's COMMIT exchange; that infrastructure-level fault remains unsimulated. The implementation relies on the durable completed idempotency record to resolve the retry. Do not interpret this test as proving every database/network failure mode.

The current commit's CI must pass before this addition is treated as verified. The PR remains open and Draft; no merge is authorized by this work.


## Verification update — lost-response retry test

**CI evidence for commit `46b88acc095c470803ab88ed354d82bace273814`: PASS.** Both .NET workflow runs passed, including the PostgreSQL migration/recovery smoke test, and both customer-mobile workflow runs passed, including TypeScript and Expo Doctor.

The new test `Retry_after_committed_order_but_lost_response_replays_original_result` verifies recovery when the transaction has committed but the caller loses the response. It does **not** inject a real connection loss during the PostgreSQL COMMIT exchange. That lower-level ambiguous-commit scenario remains **NOT PROVEN** and is not a release-blocking claim for the current application-level retry contract; it must remain visible as a verification limitation.

PR #22 remains open and Draft. No merge was performed.

## Verification update — current branch head `075ef8cb11ea03caad1114917b2607d5ad4d8efc`

**CI evidence: PASS on the current branch head.** The four GitHub Actions runs completed successfully:
- .NET PR workflow: `39440858677` — build/test and PostgreSQL migration/recovery smoke test passed.
- Customer-mobile PR workflow: `39440858723` — TypeScript check and Expo Doctor passed.
- .NET push workflow: `39440848947` — build/test and PostgreSQL migration/recovery smoke test passed.
- Customer-mobile push workflow: `39440848813` — TypeScript check and Expo Doctor passed.

This closes the CI-evidence gap for the application-level create-order idempotency changes on this head. Covered scenarios include same-key concurrent convergence, sequential replay after cart consumption, simulated lost response after a successful commit, conflicting key reuse across carts, different keys racing on the same cart, and rollback after a persistence failure.

**Still explicitly not proven:** a physical PostgreSQL/network connection failure during the COMMIT exchange; stale-reservation operational recovery; every possible failure boundary; and the eventual HTTP/API authorization and error-mapping contract. The request fingerprint remains customer + cart ID, so it models “checkout this cart once,” not a hash of immutable cart contents/prices. These are scoped limitations, not claims that all possible failure modes are covered.

PR #22 remains open and Draft. No merge was performed.


## Owner decision — email/phone identifiers and password login

**Status: OWNER-APPROVED PRODUCT DIRECTION — detailed security design and implementation gate still required.**

1. Support login using either an email address or a phone number, with a password. A user should not need to provide both identifiers to sign in.
2. Verify email and phone independently. Verification of one channel must not mark the other verified.
3. Provide secure account recovery using a verified channel, with rate limits, single-use purpose-bound expiring challenges, safe public responses, and abuse monitoring. Exact provider, expiry/attempt thresholds, and recovery policy remain open for detailed design.
4. Do not automatically merge accounts because their email addresses or phone numbers match. Any future account-linking flow must prove control of the relevant identifiers and handle conflicts explicitly.
5. One-time-code-only login is not part of the first approved login mechanism. OTP/passwordless login and MFA may be evaluated later as separate product/security decisions; do not infer approval for them from approval of email + phone.
6. This direction does not authorize implementation, new packages, database/schema changes, migrations, token settings, or PR promotion/merge. Before implementation, detail password policy and storage, verification/recovery flows, rate limits, session/token revocation, lockout/abuse controls, and privacy-safe responses, then obtain approval at the implementation gate.


## Owner decision — account activation after verification of at least one contact channel

**Status: OWNER-APPROVED PRODUCT POLICY — documentation only; implementation not authorized.**

1. **Activation threshold:** activate the account once at least one contact channel—email or phone—has been successfully verified.
2. **Second channel:** users may add the other contact channel after activation and complete its verification later. Requiring both email and phone to be verified is **not** a prerequisite for initial account activation.
3. **Independent verification:** email and phone verification states remain independent. Verifying one channel must never mark the other as verified.
4. **Use of a channel:** an email address or phone number may be used for login or recovery only after that specific channel has been verified. The approved first-release login mechanism remains password-based; OTP-only/passwordless login is not approved by this decision.
5. **Security boundaries:** account recovery must continue to use verified channels and retain purpose-bound, single-use, expiring challenges, rate limits, privacy-safe public responses, and abuse monitoring. Verification of a contact channel alone does not prove ownership of a commerce customer profile, existing order, or another account, and must not trigger automatic account merging or linking.
6. **Trade-off accepted:** this reduces signup friction and avoids blocking users who cannot immediately access one channel, while allowing them to strengthen recovery options later. The product must clearly show which channels are verified and encourage verification of the second channel without falsely presenting it as complete.
7. **Open detailed-design items:** exact verification and recovery flows, providers, challenge expiry/attempt thresholds, resend limits, password policy/storage, session and token revocation, lockout/abuse controls, privacy-safe responses, and retention remain to be designed and reviewed.

This decision supersedes the previous recommendation marked pending owner approval. **No authentication/API implementation, package/framework change, database/schema change, migration, runtime configuration, or PR promotion/merge is authorized by this decision.** The detailed security design and a separate explicit implementation gate are still required.

## Owner decision — staged sign-in methods and optional mobile biometrics

**Status: OWNER-APPROVED PRODUCT DIRECTION — documentation only; implementation not authorized.**

1. **Phased sign-in plan:** prioritize Google sign-in for an early release; design the account model to support email/password and phone/password without requiring both identifiers. Verify email and phone independently and activate an account after at least one channel is verified, as recorded above. Phone OTP delivery through SMS or WhatsApp must be evaluated against Egyptian delivery reliability, abuse controls, provider terms, and actual per-message cost before choosing a channel. Facebook sign-in is deferred until user demand justifies its integration and ongoing maintenance.
2. **MFA and full passkey sign-in:** defer mandatory MFA and a server-supported passkey sign-in method from the first release. This does not prohibit revisiting either later; do not treat local biometric unlock as MFA or as a passkey implementation.
3. **Optional device biometrics:** the product should support an optional fingerprint / Face ID / Touch ID convenience feature if implementation and testing remain proportionate. The preferred low-complexity option is to use the operating system's biometric prompt to unlock an already-established app session or a credential protected by platform secure storage. A biometric prompt by itself is **not** proof to the server that the user is authenticated and must never create a server session on its own.
4. **Privacy and security:** the app must not collect, upload, or store fingerprint/face templates. Biometric matching remains under OS/device control. Store any session/refresh credential only in platform-protected storage with appropriate access controls; honor account disablement, logout, token expiry/revocation, and credential invalidation. If the app cannot safely protect the credential or enforce session revocation, omit the convenience feature rather than persist tokens insecurely.
5. **Fallback and availability:** biometrics are opt-in and available only where the device supports them and the user has enrolled them. Keep a safe fallback to the existing sign-in flow; changing/removing device biometrics or replacing a device must not permanently lock the customer out. Do not require biometrics for all users.
6. **Cost assessment:** the OS biometric prompt/library generally has no per-authentication SMS/provider fee, but engineering, secure-storage integration, platform-specific configuration, and physical-device testing still cost time. Expo's `expo-local-authentication` provides the native prompt; Face ID testing on iOS requires a development build rather than Expo Go. Full passkeys require additional client/server credential-registration and challenge-verification work, so they remain a separate later-phase decision.
7. **Implementation boundary:** this records product direction only. It does not authorize auth/API code, package changes, secure-storage changes, database/schema/migration work, provider selection, runtime configuration, or PR promotion/merge. First approve the detailed authentication/security design and a separate implementation gate.

**Reference material reviewed:** Expo LocalAuthentication and authentication guidance (https://docs.expo.dev/versions/latest/sdk/local-authentication/ and https://docs.expo.dev/develop/authentication/); Google passkey guidance (https://developers.google.com/identity/passkeys).

## Owner-approved customer provisioning and legacy-profile conflict policy

Approved by the product owner on 2026-10-20. These are product/security rules only; they do not authorize API, identity, schema, migration, provider, package, or runtime implementation.

1. **Trusted account/profile provisioning.** Registration creates the authentication account and customer commerce profile through a trusted server-side workflow. The account and profile are distinct records. The customer-to-account mapping remains inactive until the workflow has established which customer profile belongs to the authenticated person.
2. **No contact-only ownership inference.** A matching email address or phone number is not, by itself, sufficient evidence to claim an existing customer profile, order, business, or Store. Verification of a contact channel proves control of that channel only; it does not prove ownership of historical commerce records.
3. **No automatic legacy-order claiming.** Do not move, attach, or expose existing orders to a newly registered account merely because its verified email or phone matches legacy data. Legacy records without independently verified ownership remain inaccessible through customer APIs until a controlled resolution process establishes ownership.
4. **Resolve possible duplicates before activation.** If registration finds a potentially matching existing customer profile or conflicting mapping, do not auto-link or silently create duplicate active mappings. Keep the mapping inactive, record a safe conflict/audit event, and route the case through an explicit verification and resolution process.
5. **Server-authoritative access.** Customer APIs resolve the authenticated account to one active internal customer mapping on the server. Missing, inactive, ambiguous, or conflicting mappings fail closed. Never accept a client-supplied CustomerId as proof of ownership.
6. **Unique and auditable links.** The eventual persistence design must prevent duplicate active account/customer mappings, audit creation/activation/deactivation and conflict resolution without logging credentials or tokens, and define transactional behavior for partial failures.
7. **Preserve transactional history.** Resolving a mapping must not rewrite order history or delete commerce records as a side effect. Any exceptional legacy-record ownership correction must be separately authorized, evidenced, audited, and covered by tests.

### Required follow-up design before implementation

The detailed provisioning design must define: (a) the trusted evidence required to match an existing customer profile; (b) the user-facing path for duplicate/conflicting profiles and account recovery; (c) manual support escalation and audit access; (d) transactional/outbox or compensation behavior if account creation succeeds but profile/link creation fails; (e) the exact mapping states and activation conditions; and (f) privacy-safe error messages and abuse/rate limits. These are open design details, not silently decided here.

**Decision status:** product-level rules approved. Detailed verification and conflict-resolution design remains open. No application code, database schema, migrations, or API behavior was changed by this decision record.

## Detailed provisioning workflow — proposed design for review

**Status: ARCHITECTURE PROPOSAL — not yet owner-approved for implementation.** This section turns the approved product rules above into a reviewable workflow. It intentionally does not change application behavior.

### Separate account activation from customer-data access

These are two distinct states and must not be conflated:

- **Login account activation:** allowed after at least one contact channel is verified, consistent with the approved product rule.
- **Customer mapping activation:** allowed only after a trusted server-side workflow has selected or created exactly one intended customer profile and resolved any profile conflict. Contact verification alone does not activate ownership of legacy commerce data.
- **Customer API access:** requires a valid authenticated principal, an enabled account, and exactly one active mapping to the internal customer identity. Fail closed if the mapping is missing, pending, disabled, or ambiguous.

### Proposed state model

| Object | Proposed states | Transition rule |
|---|---|---|
| Login account | PendingVerification → Active; Active ↔ Disabled | At least one contact channel verified before activation; disablement blocks login/access; re-enable requires an authorized recovery/admin path. |
| Customer provisioning | NotStarted → Pending → Completed; Pending → Conflict; Pending → RetryRequired | Server-owned, idempotent workflow; retries must not create duplicate profiles or mappings. |
| Account/customer mapping | Pending → Active; Pending → Conflict; Active → Disabled | Activate only after trusted profile selection/creation and uniqueness checks; never activate by client-supplied CustomerId. |
| Legacy ownership case | Unclaimed → UnderReview → Verified / Rejected | Separate auditable resolution; no implicit order backfill or exposure. |

The exact persisted representation (enum/status columns, workflow table, or another mechanism) is deliberately left open until schema and transaction ownership are designed.

### Recommended happy path

1. Create the authentication account with an idempotent registration request; store passwords only using the selected framework's approved password-hashing mechanism, never as reversible/plain text.
2. Start the contact verification challenge. Keep email and phone verification independent and apply rate limits/resend controls.
3. Once at least one contact channel is verified, activate the login account as approved. Do not infer customer-data ownership from this transition.
4. Run a server-side customer-provisioning operation. Use normalized contact values only as candidate-search signals for potential duplicates, not as sufficient proof to claim a profile or historical order.
5. If no conflicting customer profile exists, create a new commerce customer profile and its pending account mapping using an idempotent operation and database uniqueness constraints. If the workflow can guarantee both writes in one database transaction, commit them together; otherwise use durable workflow state/outbox-orchestration and retries.
6. Activate the mapping only after the profile and mapping are persisted, uniqueness constraints are satisfied, and the workflow confirms there is no unresolved profile conflict.
7. On each customer API request, validate the token and resolve the subject to one active mapping server-side. Do not cache mapping state in a way that bypasses account disablement or mapping revocation.

### Duplicate/conflict handling

- A possible match is a **review signal**, not an auto-link instruction.
- Do not disclose whether a particular email/phone or legacy profile exists in public registration responses; use privacy-safe generic responses.
- Keep conflicting mappings inactive. Do not create two active mappings for one login account or attach one customer profile to multiple login accounts in the first version.
- Offer a recovery path that proves control of the existing login account where possible. If recovery is impossible, use a documented support review requiring evidence beyond matching contact details alone.
- Support resolution must record actor, timestamp, reason, evidence category, previous state, and resulting state. Avoid storing excessive sensitive evidence or secrets in audit logs.
- A duplicate profile resolution and a legacy-order ownership claim are separate decisions. Resolving the profile does not automatically authorize reassignment of historical orders.

### Partial failure and retry behavior

Recommended default: a durable, idempotent provisioning workflow, because the reviewed repository does not yet establish that Identity and commerce writes share one transaction boundary.

- If account creation succeeds but profile/mapping provisioning fails, leave the account active if the contact-verification policy allows it, but keep customer commerce access unavailable until provisioning completes.
- Persist a retryable provisioning status and a correlation/idempotency identifier; retries resume the same logical operation rather than creating another customer profile.
- If a conflict is found, stop automatic retries that would repeatedly attempt to create or link profiles; route to conflict resolution.
- If profile creation succeeds but mapping activation fails, retry activation idempotently after checking the persisted profile and uniqueness constraints.
- If the account is disabled while provisioning is pending, the workflow must not activate access; the final activation step rechecks account and mapping state.
- Make failures observable through structured, privacy-safe logs/metrics and an operational path for stuck Pending/RetryRequired cases. Do not log passwords, verification codes, access tokens, refresh tokens, or raw secrets.

### Minimum tests before implementation can pass its gate

1. Verified email only and verified phone only can each activate a login account; the other channel remains unverified.
2. No mapping, pending mapping, disabled account, disabled mapping, duplicate mapping, or ambiguous subject all deny customer API access.
3. A fresh profile and mapping are created once under repeated/concurrent registration retries.
4. A candidate existing profile never gets auto-linked based only on email/phone equality.
5. A conflict remains inaccessible until an explicit resolution is recorded.
6. A legacy order with null/unverified ownership is not returned by list/detail endpoints and is not reassigned by registration.
7. Failure after account creation, after profile creation, and before mapping activation can be retried without duplicates or partial access.
8. Account disablement racing with provisioning prevents mapping activation/access.
9. Public responses do not reveal whether an account, profile, or order exists; verification/recovery endpoints enforce expiry, single use, and rate limits.
10. Audit events cover mapping activation/deactivation and conflict resolution without sensitive credential/token data.

### Decisions still needed before implementation

Owner review is required for: (1) whether support-assisted resolution is in the first release or all unresolved conflicts remain blocked; (2) what evidence categories support may accept to resolve a legacy profile/order claim; (3) whether the Identity and commerce stores will share a database/transaction or use a durable orchestration workflow; and (4) the operational owner and SLA for stuck provisioning cases.

**Recommendation:** approve the state separation, fail-closed API rule, idempotent provisioning workflow, and no-contact-only auto-link rule as design direction. Keep legacy-order reassignment as a separate exceptional process. Do not start implementation until storage/transaction ownership and conflict-resolution evidence are decided and a separate implementation gate is explicitly approved.

## Repository evidence review — persistence and transaction boundary

**Review status: VERIFIED FROM THE CURRENT BRANCH TREE AND SOURCE FILES. This is an architecture finding, not implementation approval.**

Reviewed branch: `integration/baladna-foundation-mobile`  
Reviewed head before this note: `793a241ae98861eee567a2f4a9e0b3d7fcfec4a2`

### Confirmed repository facts

1. The only current EF Core context found in the source tree is `LocalCommerce.Infrastructure.Persistence.CommerceDbContext`. It maps commerce entities such as Orders, OrderItems, Carts, Deliveries, Stores, Products, and IdempotencyRecords. It does not map an authentication account, customer profile, or account/customer mapping entity.
2. The Infrastructure project targets `.NET 8` and references EF Core `8.0.31` plus Npgsql EF Core provider `8.0.11`. The Application project also targets `.NET 8` and references Domain, not EF Core.
3. The current migration resources are embedded SQL scripts `001_initial.sql` and `002_order_customer_ownership.sql`. `DatabaseInitializer.InitializeAsync` discovers and executes the embedded scripts in ordinal name order through the same `CommerceDbContext.Database` connection. In the reviewed initializer and migration files, no migration-history/checksum ledger or durable provisioning-workflow table is defined.
4. `Orders.CustomerId` is nullable and indexed, but the reviewed SQL does not define it as a foreign key to a customer table. Migration `002` explicitly preserves legacy orders as unowned unless verified backfill data exists.
5. The current source tree contains no API host/ `Program.cs`, ASP.NET Core Identity store/context, OpenIddict store/context, customer-profile table/entity, or account/customer mapping implementation. These remain future design/implementation work.
6. The existing persistence design gate documents transactional idempotency for Create Order. That transaction boundary is specific to the commerce checkout path; it does not establish a transaction spanning a future identity store and commerce persistence.

### Consequence for the provisioning decision

**A shared Identity + commerce database transaction is not currently available to rely on:** the reviewed branch has no implemented identity store, customer-profile persistence, or mapping persistence to join to the existing commerce transaction. Merely choosing PostgreSQL for both later would not prove that separate EF contexts/stores share one connection and transaction.

**Recommended direction for the first implementation design:**
- Treat the identity account and commerce customer/mapping as separate persistence responsibilities.
- Design provisioning as a durable, idempotent workflow with explicit states and observable retries, so partial failure cannot grant customer-data access.
- Use a local database transaction for the commerce-side customer-profile/mapping writes once their schema and ownership are defined. If identity and commerce are deliberately consolidated under one shared transaction in a future design, require evidence of shared connection/transaction ownership before changing this recommendation.
- Keep customer API authorization fail-closed until the server can resolve exactly one active mapping.
- Decide whether workflow state/outbox belongs in the commerce database or a dedicated workflow store only after the API host, identity persistence, deployment topology, and operational recovery ownership are designed.

This is a recommended architecture based on the current repository evidence, **not an owner-approved implementation decision**. No code, schema, migration, package, or runtime configuration was changed as part of this review.

## Conflict resolution and stuck-provisioning operations — proposed defaults

**Status: PROPOSAL FOR OWNER REVIEW. These defaults are not approved implementation requirements yet.**

The current repository confirms the commerce persistence boundary but does not yet define a customer-support console, an identity administration surface, or an operational provisioning queue. The first release should therefore prefer a small, safe process over a broad manual account-editing capability.

### Proposed first-release conflict policy

1. **Default for unresolved profile conflicts: remain blocked.** Keep the mapping inactive and customer commerce access unavailable until resolution. Do not auto-create a second active mapping to bypass the conflict.
2. **Support-assisted account recovery may be in scope; manual ownership override should not be.** Support can guide a customer through recovery of an existing account using approved verified-channel recovery. Support must not be able to mark an account/contact as verified, activate a mapping, or claim an old order merely by editing a field.
3. **Legacy-order claims should be out of scope for ordinary registration support.** If a legacy order has no independently established owner, keep it unclaimed. Any future claim workflow should be separately designed, narrowly permissioned, reviewed by a second authorized person for exceptional cases, and fully audited.
4. **Do not collect excessive evidence.** Record an evidence category and review outcome; avoid retaining identity documents or sensitive attachments unless a separate retention/access policy and legal basis are established.
5. **Privacy-safe customer messaging.** Public responses should not reveal whether a matching profile or order exists. The authenticated recovery path may provide next steps without disclosing unrelated account or order details.

### Proposed evidence standard

Evidence must be stronger than contact equality alone. A verified email/phone may establish control of that channel but not ownership of an old commerce profile or order.

For profile recovery, prefer proof of control of the already-existing account or an established recovery mechanism. For a legacy order claim, do not accept only a matching name, email, phone, or order number as sufficient. Require a separately designed evidence policy before implementation; the initial safe default is **no legacy-order reassignment in the first release**.

### Proposed operational handling for pending work

- Persist a stable provisioning operation identifier, state, attempt count, timestamps, and a non-sensitive failure category.
- Use bounded retry with backoff for transient infrastructure failures. Do not automatically retry unresolved conflicts as if they were transient.
- Make retries idempotent and enforce database uniqueness; retries must resume the same logical operation.
- Expose operational metrics for pending age, retry count, conflict count, and last failure category without exposing credentials, contact challenge codes, tokens, or unnecessary personal data.
- Proposed initial alert threshold: provisioning still pending/retry-required after 15 minutes. This is a starting operational target, not a guarantee; tune it to the eventual hosting/queue model and support coverage.
- Assign an operational owner before launch. If no staffed support/operations path exists, unresolved conflicts and stuck provisioning remain blocked and must not be resolved through direct database edits.

### Proposed decision record for owner approval

| Decision | Recommended default | Why |
|---|---|---|
| Support intervention in first release | Recovery guidance only; no direct mapping/ownership override | Limits privileged manual access and accidental cross-account exposure |
| Legacy order ownership | No reassignment in first release | No existing evidence policy or ownership-verification flow is implemented |
| Stuck workflow | Durable state, bounded retries, metrics/alerts, manual review without direct DB edits | Makes partial failures visible and prevents silent indefinite pending states |
| Operational target | Alert after 15 minutes pending, then review against real service coverage | Gives a measurable initial threshold without promising immediate human resolution |
| Conflict resolution | Inactive until approved, auditable resolution flow exists | Fails closed and prevents account/contact matches from becoming ownership proof |

**Owner review is still required before implementation.** This proposal does not authorize a support console, workflow worker, new schema, migration, API, or authentication code.



## Customer provisioning persistence design — next review slice

**Status: PROPOSED DESIGN — not owner-approved for implementation.** This section narrows the remaining persistence choices using the verified current repository boundary. It does not change application code or authorize schema, package, framework, API, or migration work.

### Recommended first-release persistence shape

Keep the authentication account in ASP.NET Core Identity and commerce customer ownership in the commerce persistence boundary. Do not make the Identity user ID double as the commerce `CustomerId`.

| Record | Purpose | Minimum proposed fields / constraints |
|---|---|---|
| Commerce customer profile | Customer-facing commerce identity/profile | `CustomerId` (stable UUID primary key), profile fields only when needed, lifecycle status, created/updated timestamps |
| Account/customer mapping | Trusted link from one Identity account to one commerce customer | Identity subject/user ID (unique), `CustomerId` (unique), status, created/activated/disabled timestamps, correlation ID; enforce one account ↔ one customer for the first release |
| Provisioning operation | Durable retry and diagnosis of the account-to-profile workflow | stable operation ID, Identity subject when known, operation type, state, attempt count, created/updated/next-attempt timestamps, safe failure category, correlation ID; unique idempotency key scoped to operation type/account-registration intent |
| Conflict/audit event | Trace conflict detection and authorized resolution without exposing secrets | event ID, operation ID, event type, actor type/ID where applicable, timestamp, reason/evidence category, previous/new state; no passwords, tokens, verification codes, or unnecessary raw personal evidence |

This is a logical model, not final SQL. Exact names, field types, indexes, retention, and whether conflict events share the audit table remain open until the API host and persistence ownership are designed.

### Mapping and database invariants

1. An Identity subject can have at most one active customer mapping; for the initial one-person/one-customer model, enforce uniqueness on the subject across all mapping states unless a separately approved lifecycle requires historical mapping rows.
2. A commerce `CustomerId` can be mapped to at most one Identity subject in the first release. Do not support shared family accounts or account merging implicitly.
3. Mapping status is not inferred from account status. Customer API authorization requires both an enabled authenticated account and exactly one active mapping.
4. Create the customer profile and pending mapping in one local commerce database transaction. Activate the mapping only after the workflow rechecks uniqueness, unresolved conflicts, and account eligibility.
5. Do not add a foreign key from legacy `Orders.CustomerId` until the customer table, historical-data compatibility, and migration/backfill strategy are explicitly designed. Existing null/unverified ownership remains inaccessible.
6. Database uniqueness constraints are the final concurrency guard. Application-level “check then insert” logic alone is insufficient.

### Durable operation state and recovery

Proposed states: `Pending`, `Running`, `RetryRequired`, `Conflict`, `Completed`, `Cancelled`.

- `Pending → Running`: a worker or bounded application operation claims the durable work using concurrency-safe state transition/lease semantics.
- `Running → Completed`: the profile and mapping exist exactly once, the mapping is active, and account eligibility has been rechecked.
- `Running → RetryRequired`: a classified transient infrastructure failure occurred; retry with bounded exponential backoff and jitter.
- `Pending/Running → Conflict`: candidate-profile ambiguity or a uniqueness conflict requires explicit resolution; do not retry it as a transient error.
- `RetryRequired → Running`: retry the same operation ID and idempotency identity, never create a new logical operation.
- `Pending/Running/RetryRequired → Cancelled`: only a defined administrative/system cancellation path; cancellation never deletes an already-created profile or rewrites orders.

Persist attempt count, last safe failure category, last-attempt time, and next-attempt time. Avoid persisting exception text that may contain secrets or personal data. A recovery sweep should identify stale `Running` operations by lease/heartbeat expiry and return them to retryable processing safely; a process restart must not leave them permanently stuck.

**Important boundary:** a durable table alone is not a complete background-processing mechanism. Before implementation, decide whether the first release uses a bounded, idempotent request-driven operation plus a recovery sweep, or a hosted worker. Do not add a broker or distributed infrastructure without evidence that a simpler hosted worker is insufficient.

### Identity/commerce partial-failure matrix

| Failure point | Required safe result | Retry behavior |
|---|---|---|
| Identity account creation fails | No customer mapping; no customer data access | Retry registration safely under the same logical request |
| Identity account created, verification pending | Account remains pending per verification policy; no active customer mapping | Resume verification; provisioning must not grant commerce access |
| Account is active but provisioning operation has not completed | No customer API access until an active mapping exists | Resume/reconcile by stable operation/correlation identity; do not duplicate profile |
| Profile insert succeeds but mapping write fails | Local commerce transaction rolls back both writes | Retry the same operation |
| Profile + pending mapping commit, then process crashes | Mapping remains inactive | Recovery finds the persisted operation and resumes activation idempotently |
| Account is disabled before mapping activation | Mapping must not become active | Stop activation; mark operation blocked/cancelled according to the approved lifecycle |
| Duplicate candidate profile or uniqueness conflict | No automatic link and no active mapping | Mark Conflict and route to the approved resolution path |
| Activation commits but client loses response | One active mapping at most | Read/retry the same operation and return a safe status; do not create a second profile |
| Two registration/provisioning attempts race | At most one profile/mapping pair for the logical operation and unique mapping constraints hold | One winner; other attempt reads the durable outcome or reports a privacy-safe pending/conflict result |

The identity store and commerce store do not currently share a demonstrated transaction. Therefore, cross-store atomicity must not be claimed. Safety comes from inactive-by-default mappings, durable operation identity, local commerce transactions, uniqueness constraints, and retry/reconciliation.

### Conflict and operational defaults recommended for owner review

- **Unresolved profile conflicts:** remain blocked; no automatic second active profile/mapping to bypass a possible duplicate.
- **Support in the first release:** recovery guidance only; no direct support override that marks contact verification, activates mappings, or claims old orders.
- **Legacy-order claims:** out of scope for ordinary registration and support in the first release. Any later exception needs a separate evidence policy, narrow authorization, second-person review, and audit.
- **Stuck provisioning:** alert for operations pending/retry-required beyond the proposed 15-minute initial threshold; treat this as a tunable operational target, not a service-level guarantee.
- **Operations ownership:** name an accountable operational owner and define the review path before launch. If no authorized resolution path exists, keep the mapping inactive rather than editing production rows directly.

These are recommendations only. The owner still needs to approve or amend the support scope, evidence categories, processing mechanism, and operational ownership before implementation.

### Additional acceptance tests required by this persistence design

1. Unique constraints reject a second mapping for the same Identity subject and a second subject for the same CustomerId.
2. Concurrent provisioning requests for the same logical operation create one profile and one mapping.
3. A failure between profile and mapping persistence rolls back both commerce writes.
4. Crash after persisting a pending mapping is recovered without activating it twice or creating duplicates.
5. An account disabled between operation start and activation never receives an active mapping.
6. Stale-running recovery is concurrency-safe; two recovery workers cannot activate or provision twice.
7. A conflict is durable, privacy-safe, and excluded from automatic transient retries.
8. Registration responses do not reveal whether a candidate profile or legacy order exists.
9. Legacy orders with null or unverified ownership remain excluded from customer list/detail responses.
10. Retried completion after a lost response returns the same operation/profile outcome.
11. Audit events record activation, disablement, conflict, and resolution with actor/time/reason category but no secrets.
12. Tests verify that inactive, missing, disabled, or ambiguous mappings fail closed at the API authorization boundary once the API host exists.

### Decisions required to close this part of the design gate

| Decision | Recommended default | Current status |
|---|---|---|
| Persistence location for provisioning state | Commerce PostgreSQL, because profile/mapping writes are commerce-owned; keep Identity as separate account store | Open |
| Processing mechanism for first release | Start with a small hosted worker/recovery loop only if deployment guarantees a single durable service process; otherwise choose a durable scheduler/queue after topology is known | Open |
| Support intervention | Recovery guidance only; no ownership override | Open |
| Legacy order reassignment | Not in first release | Open |
| Stuck-operation alert target | 15 minutes as an initial operational threshold | Open |
| Operational owner / response expectation | Must be named before launch; do not assume staffed support | Open |

**Gate status remains: design review required; implementation not authorized.** The next design task is to resolve these decisions and then define the exact API contracts for registration, verification, provisioning status, recovery, and customer-order access. No authentication or database implementation should begin until the owner explicitly approves the implementation gate.


## First customer API slice — contract proposal

**Status: PROPOSED CONTRACT; no API host or endpoints are being implemented by this document.** The route shapes below are intentionally illustrative until the identity provider, API host, token validation, and deployment topology are approved.

### Scope and non-goals

In scope for the first authenticated vertical slice:
- safe liveness/readiness health checks that disclose no secrets or infrastructure details publicly;
- registration initiation and contact-verification flow through the approved identity provider;
- authenticated provisioning status for the caller's own account;
- customer order list and order detail, with ownership derived only from the authenticated subject and active server-side mapping.

Out of scope unless separately approved: merchant, store-management, driver, admin, payment/refund, customer-profile claiming, legacy-order reassignment, public catalog, and broad service-request endpoints. Do not expose a generic endpoint that accepts a caller-provided `CustomerId` as an ownership credential.

### Proposed routes and contracts

| Method / route | Authentication | Proposed behavior | Key safety rule |
|---|---|---|---|
| `GET /health/live` | Public | Confirms process is alive | No database credentials, provider state, exception text, or internal topology |
| `GET /health/ready` | Deployment-policy dependent | Confirms required dependencies are usable | Return only a generic readiness result publicly; detailed diagnostics stay in protected telemetry |
| `POST /api/v1/identity/registrations` | Public, rate-limited | Initiates account registration using the approved sign-in/provider direction | Idempotency and privacy-safe response; never claim commerce ownership |
| `POST /api/v1/identity/verification` | Public, rate-limited | Starts or confirms a purpose-bound verification challenge, depending on provider design | Never accept a client assertion as proof without server/provider verification |
| `GET /api/v1/customer/provisioning-status` | Authenticated | Returns the caller's provisioning state | Resolve account subject from validated token; never accept subject/customer IDs from query/body |
| `GET /api/v1/customer/orders` | Authenticated | Lists orders belonging to the caller's active mapped customer | Missing/inactive/ambiguous mapping fails closed |
| `GET /api/v1/customer/orders/{orderId}` | Authenticated | Reads one order after resource-ownership check | Use the same ownership predicate as list; random IDs are not authorization |

Registration and verification route details must be reconciled with the selected Identity/OpenIddict flow before implementation. If the identity provider owns a protocol endpoint, do not duplicate it with a custom application endpoint without a documented need.

### Request and response shape principles

- Use dedicated request/response DTOs; never bind EF entities or Identity entities directly.
- Registration input may include only the identifier(s) required by the chosen flow and must be validated/normalized consistently. Never accept `CustomerId`, role, mapping status, account activation status, or verification status from the client.
- Return a correlation identifier on accepted operations and a safe status such as `PendingVerification`, `Provisioning`, `ActionRequired`, or `Ready`; do not return candidate customer data or disclose whether an old profile/order exists.
- The provisioning-status response is scoped to the validated caller and should expose only actionable state, for example `state`, `nextAction`, and a non-sensitive `correlationId`. Do not expose raw internal errors, provider responses, retry stack traces, or another person's profile identifiers.
- Order responses contain only customer-authorized order fields. Do not leak internal notes, other customers' data, driver personal data, secrets, or internal exception details.
- Standardize error envelopes with a stable machine-readable code, safe human-readable message, and correlation/trace identifier. Exact DTO names and JSON casing remain subject to the repository's established conventions.

### Error mapping proposal

| Condition | HTTP behavior proposal | Notes |
|---|---|---|
| Malformed or invalid input | `400` | Field-level errors only where they do not reveal account existence |
| Unauthenticated / invalid token | `401` | Do not reveal token-validation internals |
| Authenticated but no active customer mapping | `403` or a deliberately designed safe `409` workflow response | Choose one consistent contract; never fall back to caller-supplied CustomerId |
| Order does not exist or is not owned by caller | `404` | Avoid confirming another customer's order exists |
| Rate limit exceeded | `429` | Include retry guidance only when safe and supported |
| Idempotency key reused with different logical request | `409` | Stable error code; do not mutate the original operation |
| Temporary dependency/storage failure | `503` where appropriate | Safe message, correlation ID, retry only when semantics allow |
| Unexpected server failure | `500` | Generic public response; detailed exception remains in protected logs |

The exact choice between `403` and a safe provisioning workflow response for a valid account without a mapping is still an owner/API-contract decision. Avoid status differences that allow an unauthenticated caller to enumerate registrations or legacy records.

### Idempotency and rate-limit boundaries

- Apply an idempotency key to registration/provisioning mutations where the client may safely retry after a lost response. Scope it to the authenticated subject when available and the operation type; for unauthenticated registration, use a server-defined privacy-safe scope and bounded retention.
- Store a request fingerprint for the logical intent. Same key + same intent returns the durable prior outcome; same key + different intent returns a conflict without replacing the original result.
- Never place raw email, phone, verification codes, passwords, access tokens, or refresh tokens in an idempotency key, URL, logs, or metric labels.
- Rate-limit registration, resend, verification, recovery, and failed authentication independently. Exact thresholds require threat-model and provider review; do not invent fixed production limits in the API contract.
- Avoid response timing or message differences that expose whether an email, phone, profile, or order exists.

### Authorization decision procedure for customer orders

Every customer order read must follow this server-side sequence:
1. Validate token issuer, audience, signature, expiry, and applicable revocation/session rules.
2. Obtain the immutable account subject from the validated principal; ignore client-supplied subject/customer identifiers.
3. Confirm the account remains eligible for access under the chosen disablement/revocation mechanism.
4. Resolve exactly one active account-to-customer mapping.
5. Query the order with both the requested order identifier and the resolved `CustomerId` ownership predicate in the database query.
6. Return the minimal DTO; use the same rule for list, detail, pagination, counts, exports, and future customer-order operations.
7. Emit a correlation-aware audit/security event for denied access without logging tokens or unnecessary personal data.

A mapping check followed by an unconstrained order lookup is not acceptable. Resource ownership must be enforced at the data access boundary to reduce time-of-check/time-of-use mistakes. Missing, inactive, duplicated, or inconsistent mappings must fail closed and produce protected diagnostics.

### Contract and security acceptance tests

1. Anonymous callers cannot access customer order list/detail or provisioning status.
2. A valid token with no mapping cannot read orders, even when the request includes a guessed `CustomerId`.
3. A mapping for customer A cannot read customer B's order by changing `orderId`.
4. List, detail, pagination totals, and any included counts all use the same ownership predicate.
5. Null-owned legacy orders never appear in customer results.
6. Disabled accounts and revoked/expired sessions are denied according to the documented revocation latency contract.
7. Registration and verification responses do not disclose whether an identifier, candidate profile, or historical order exists.
8. Same idempotency key and same request replay a safe stable outcome; changed request conflicts without changing the original operation.
9. Rate limits cover registration, resend, verification, recovery, and repeated failures without leaking account existence.
10. Public errors contain stable codes and correlation IDs but no exception, SQL, token, provider, or secret details.
11. Health endpoints do not reveal connection strings, database names, dependency topology, or secret/configuration values.
12. Every denied ownership attempt is observable through protected logs/audit events without logging credentials or raw verification material.

### Required design closure before implementation

The API design gate cannot close until these are documented and reviewed:
- exact identity provider/protocol responsibilities and callback/redirect URI policy;
- token issuer/audience, access-token lifetime, refresh/session strategy, revocation and account-disablement latency;
- registration/verification/recovery contracts, safe responses, rate-limit policy, and provider delivery cost/reliability;
- the final no-mapping HTTP behavior and standardized error envelope;
- host/deployment topology and the durable provisioning/recovery mechanism;
- migration/backfill plan for nullable legacy `Orders.CustomerId` and proof that no unverified historical order is exposed;
- test environment strategy for PostgreSQL, identity flows, API authorization, and mobile integration.

**Current gate outcome: NOT READY FOR IMPLEMENTATION.** This contract proposal narrows the design, but does not authorize creating the API host, adding Identity/OpenIddict packages, changing database schema, or connecting mobile login to a live backend.


## Repository-grounded compatibility and mobile session review

**Review date:** 2026-10-20. This section records source-level facts observed on the current branch and identifies checks still required; it is not a package-compatibility verdict.

### Facts verified in the current branch

- The domain, application, and infrastructure project files currently target `net8.0`.
- Infrastructure currently references Entity Framework Core `8.0.31` and Npgsql EF Core provider `8.0.11`.
- No ASP.NET Core API host, ASP.NET Core Identity configuration, or OpenIddict integration was identified in the current source tree reviewed for this gate. This is a scoped repository observation, not proof that no code exists in every branch.
- The customer mobile app uses Expo SDK `~57.0.16`, React Native `0.86.3`, and TypeScript `~6.0.3`.
- The current `apps/customer-mobile/app/auth.tsx` is explicitly a prototype: Google and email/password actions show a “not connected” message and do not submit credentials. This is the correct fail-safe prototype behavior, not a completed login implementation.
- The configured GitHub Actions runs for commit `9f27c96978ccac14d98e8ccd01c2b528149f62de` completed successfully: [.NET workflow](https://github.com/palestiny/local-commerce-platform/actions/runs/39468154318) and [customer-mobile workflow](https://github.com/palestiny/local-commerce-platform/actions/runs/39468154476). The duplicate push-triggered runs also succeeded: [.NET](https://github.com/palestiny/local-commerce-platform/actions/runs/39468145871) and [customer-mobile](https://github.com/palestiny/local-commerce-platform/actions/runs/39468145720). These are the configured checks only; they do not test live auth, device secure storage, provider callbacks, or mobile-to-API integration.

### Latest verification and integration review — 2026-10-20

The exact current head `b230ccad9ef20e1bcf92054a97c1268736c1674b` has now completed all four configured GitHub Actions runs successfully:

- [customer-mobile pull-request run](https://github.com/palestiny/local-commerce-platform/actions/runs/39469482222) — success.
- [.NET pull-request run](https://github.com/palestiny/local-commerce-platform/actions/runs/39469482291) — success.
- [customer-mobile push run](https://github.com/palestiny/local-commerce-platform/actions/runs/39469475753) — success.
- [.NET push run](https://github.com/palestiny/local-commerce-platform/actions/runs/39469475977) — success.

This confirms the configured CI jobs on this exact commit only. It does not prove live authentication, device/emulator behavior, Android/iOS secure-storage behavior, RTL/LTR visual correctness, or mobile-to-API integration.

PR #22 remains **open and Draft**, with 83 changed files. Its base is `fix/replacement-delivery-store-consistency`, not protected `main`. The PR's first integration commit is based on the base branch's then-current commit, so the non-main base appears intentional for this integration candidate; however, it remains an owner-visible release-path decision and must not be silently retargeted. Before merge, review the full combined scope (mobile app, numerous HTML design references, order-ownership migration, CI workflows, and design docs) against the intended integration target and confirm whether a separate follow-up PR into `main` is required.

**Gate status remains NOT READY FOR IMPLEMENTATION.** Passing CI does not close the identity/provider, runtime/package compatibility, provisioning recovery, API contract, or deployment-topology decisions below.

### Framework and package decision — must be evidence-led

The constitution records .NET 10 LTS as the target direction, but the reviewed projects still target .NET 8. Therefore, adding Identity/OpenIddict directly to this branch would create a decision gap unless the target framework and package matrix are first reconciled.

Before implementation, produce a compatibility table for:
- selected target framework and installed .NET SDK used by CI;
- ASP.NET Core Identity and EF Core versions;
- OpenIddict server/validation package versions and supported target frameworks;
- Npgsql EF Core provider compatibility with the selected EF Core major version;
- database migration and existing embedded SQL migration behavior;
- the mobile OAuth/OIDC client library and Expo SDK 57 / React Native compatibility;
- deployment runtime availability and the support policy for the chosen framework.

Do not upgrade the solution to .NET 10, mix EF Core major versions, or add auth packages as part of a documentation-only change. If the target is changed, treat it as a separate, focused compatibility work item with a full-solution build, all tests, PostgreSQL migration smoke tests, and rollback notes. A successful existing .NET 8 workflow does not establish .NET 10 compatibility.

### Official-source compatibility findings — 2026-10-20

This is a source-based compatibility assessment, not a successful restore/build against a proposed new package set.

| Component | Verified evidence | Decision implication |
|---|---|---|
| .NET runtime lifecycle | Microsoft's release policy lists .NET 10 LTS supported until November 2028 and .NET 8 LTS until November 2026 ([official lifecycle](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support)). | Prefer .NET 10 for the new API direction rather than building a new authentication host on a runtime approaching end of support. Existing projects remain net8.0 until a separately verified migration is approved. |
| EF Core | EF Core 10 is LTS and requires the .NET 10 SDK/runtime; it will not run on earlier .NET versions ([official EF Core 10 notes](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew)). | A net10.0 migration must move EF Core packages together to the EF Core 10 major line; do not mix EF Core 8 and EF Core 10 in the same provider/context stack. |
| Npgsql EF Core provider | Npgsql publishes a 10.0 provider release aligned with EF Core 10 ([official Npgsql 10.0 release notes](https://www.npgsql.org/efcore/release-notes/10.0.html)). | The current provider 8.0.11 is the existing net8/EF8 baseline, not the target package for a net10/EF10 context. Validate the exact provider patch and PostgreSQL support matrix during the dedicated upgrade. |
| OpenIddict ASP.NET Core integration | Official OpenIddict integration docs explicitly list ASP.NET Core 10 / .NET 10 as supported. The current docs show OpenIddict 7.x package references and EF Core integration ([ASP.NET Core support](https://documentation.openiddict.com/integrations/aspnet-core), [EF Core integration](https://documentation.openiddict.com/integrations/entity-framework-core)). | OpenIddict is technically viable in a .NET 10 design, but this does not establish that an arbitrary package combination is compatible. Select matching stable package versions, check advisories/release notes, then restore/build/test in CI. Avoid adopting 8.0 preview for the first production authentication slice. |
| Native mobile OIDC client | Current app dependencies show Expo SDK 57 / React Native 0.86.3, but no native OIDC client or secure credential storage dependency is currently listed. | Still OPEN: choose a maintained library compatible with the actual Expo SDK and development-build/native-module requirements; prove redirect handling and platform-backed secure storage on Android and iOS before connecting real login. TypeScript/Expo Doctor alone cannot prove these behaviors. |

### Recommended compatibility decision for owner review

- **Target for the new API/auth host:** .NET 10 LTS.
- **ORM/provider family for that host:** EF Core 10 + the matching stable Npgsql EF Core 10 provider.
- **Protocol stack:** a currently supported stable OpenIddict release whose ASP.NET Core and EF Core integration assets match the chosen runtime and ORM major versions.
- **Migration strategy:** keep the existing net8.0 commerce projects and current migrations unchanged in a focused design/compatibility branch until the owner approves a solution-wide or carefully isolated target-framework plan. Do not partially retarget projects or run two EF major versions through one shared context.
- **Proof required before declaring the matrix closed:** exact package versions pinned; clean restore; full solution build/tests; PostgreSQL migration/recovery smoke test against a representative legacy schema; security tests for issuer/audience/signature/expiry, disablement and refresh revocation; Android and iOS callback and secure-storage smoke tests. Record exact run links and commit SHA.

The official sources support the direction above, but do not decide the application's deployment topology, migration ownership, native client choice, or access-token revocation latency. Those remain OPEN.

### Proposed mobile authentication/session baseline

These defaults are recommended for review, not yet owner-approved implementation settings:

1. Use a standards-based Authorization Code flow with PKCE for the native app if the chosen provider and deployment architecture support it. Do not embed a client secret in the mobile binary.
2. Use the system browser/provider-supported native flow and validate exact redirect URI schemes/hosts. Reject open redirects and do not accept arbitrary callback URLs.
3. Keep access tokens short-lived; select the actual lifetime only after threat-model and provider review. If refresh tokens are used, require rotation/reuse detection where supported and define revocation behavior explicitly.
4. Store refresh/session credentials only in platform-backed secure storage with appropriate iOS Keychain and Android Keystore-backed behavior. Do not put credentials in AsyncStorage, ordinary app state persistence, URLs, logs, analytics, or crash reports.
5. Biometrics, if later approved, may gate local access to a protected credential/session; they are not proof to the server and do not replace token validation, MFA, or revocation. If secure storage or lifecycle handling is unavailable, omit the feature.
6. Logout must clear local credentials and request server-side session/refresh-token revocation where supported. Local deletion alone must not be described as revoking a stolen token.
7. Account disablement must block new sign-ins and refreshes and meet a documented maximum access-revocation latency. If self-contained access tokens cannot be revoked immediately, define a server-side account/session validation strategy for sensitive endpoints rather than promising instant invalidation.
8. Never log tokens, passwords, OAuth authorization codes, verification codes, or raw provider payloads. Scrub sensitive fields from structured telemetry and mobile crash reports.

### Authentication sequence to test before enabling real login

1. Mobile opens the approved provider authorization endpoint using Authorization Code + PKCE.
2. Provider returns only to a registered redirect URI; state and PKCE correlation are checked.
3. The app exchanges the code using the selected supported client flow; no client secret is shipped in the app.
4. The API validates issuer, audience, signature, expiry, and applicable session/account state.
5. The API resolves the authenticated subject to exactly one active commerce mapping before any customer resource is returned.
6. The app persists credentials only in approved secure storage and displays pending provisioning separately from successful customer readiness.
7. Logout, refresh rotation, expired token, disabled account, provider outage, lost response, and reinstall/recovery paths are tested.
8. Tests prove that a UI navigation state or locally stored customer ID cannot bypass API authorization.

### Remaining decisions (owner review required)

| Decision | Recommended direction | Status |
|---|---|---|
| Runtime target for the first API host | Reconcile constitution's .NET 10 target with current net8.0 projects in a separate compatibility decision | Open |
| Identity and protocol package versions | Select only after official compatibility matrix and CI runtime are verified | Open |
| Native OAuth/OIDC client and callback registration | Authorization Code + PKCE, no embedded secret; choose library after Expo compatibility check | Open |
| Access/refresh token lifetime and revocation | Short access-token lifetime; rotation/reuse detection if refresh tokens are used; define disablement latency | Open |
| Secure credential storage | Platform-backed secure storage; do not use ordinary persistent app storage | Proposed |
| Login providers for first release | Google priority; email/password and phone/password subject to provider/verification design; Facebook deferred as previously agreed | Previously agreed direction; implementation open |
| Biometric unlock | Optional and local-only; omit if secure session storage/lifecycle is not robust | Previously agreed direction; implementation open |

**Gate status remains NOT READY FOR IMPLEMENTATION.** The next technical design output should be the package/runtime compatibility matrix and the exact OAuth/OIDC sequence for the chosen identity provider, followed by owner review of unresolved defaults. No package, framework, API, schema, or mobile-auth implementation is authorized by this section.


### Expo 57 authentication library evidence — 2026-10-20

This is a documentation-only dependency assessment; no package was installed and no device flow was run.

- Expo's SDK reference confirms SDK 57 targets React Native 0.86, matching the repository's recorded React Native 0.86.3 line ([SDK reference](https://docs.expo.dev/versions/latest/)).
- Expo's OAuth/OIDC guide documents `expo-auth-session` for browser-based OAuth/OpenID Connect across Android, iOS, and web, including redirect URI generation and PKCE support ([Expo authentication guide](https://docs.expo.dev/guides/authentication/), [AuthSession reference](https://docs.expo.dev/versions/latest/sdk/auth-session/)).
- Expo documents `expo-secure-store` as platform-backed storage using Android encrypted preferences and iOS Keychain, and explicitly distinguishes it from AsyncStorage ([Expo storage/auth guidance](https://docs.expo.dev/guides/authentication/)).
- Expo also notes that some provider-specific native SDKs require custom native code and a development build; its Google authentication guide identifies native-library choices and this build requirement ([Google authentication guide](https://docs.expo.dev/guides/google-authentication/)).

#### Recommended first validation path

1. Evaluate Expo's maintained `expo-auth-session` as the initial generic OIDC client candidate for the app's Authorization Code + PKCE flow. This is a candidate, not yet a locked dependency: verify the exact SDK-57-matched package versions with the repository's Expo install tooling and CI before adoption.
2. Evaluate `expo-secure-store` for native credential persistence, but do not store provider access tokens as a substitute for the application's own API session model. Define precisely which app-issued refresh/session credential is persisted and how it is revoked.
3. Because Google is the first-priority social provider, compare the generic browser OIDC route with Expo's documented native Google SDK options. Prefer one coherent, tested flow rather than combining two token/session models. Provider choice and callback registration remain open.
4. Plan an Expo development build and real-device Android/iOS smoke tests; Expo Go or TypeScript checks alone are not evidence that registered callbacks, native modules, Keychain/Keystore behavior, reinstall, logout, and revocation work in production.
5. Pin exact dependency versions only after verifying the SDK 57 compatibility matrix and then run clean install, typecheck, lint, and CI. Do not infer that latest-version documentation proves compatibility with SDK 57.

**Decision status:** `expo-auth-session` + `expo-secure-store` is the recommended first candidate to validate, not an approved implementation or proven runtime combination. The auth design gate remains **NOT READY FOR IMPLEMENTATION** until owner review and the required package/device/security evidence are recorded.


### Logout, refresh, and account-disablement policy — decision proposal

**Purpose:** make session invalidation a measurable contract before choosing token settings or writing auth code. The current mobile app has no live session flow, so these are design options and acceptance criteria, not implemented behavior.

#### Options considered

| Approach | Benefit | Cost / limitation | Assessment |
|---|---|---|---|
| Short-lived self-contained access token + revocable rotating refresh token | Low API lookup overhead for ordinary requests; refresh can be stopped centrally | An already-issued access token can remain usable until expiry unless the API checks server-side account/session state; rotation and replay detection add state and tests | Reasonable baseline only if the accepted disablement window is explicit |
| Server-checked session/account state on every sensitive request | Can enforce disablement and session revocation quickly and consistently | Adds database/cache dependency and latency; cache invalidation and outage behavior must be designed | Recommended for customer/order/payment-sensitive APIs if fast disablement is a product requirement |
| Long-lived self-contained access token with no server-side session check | Simple request validation | Logout or account disablement cannot reliably invalidate an already-issued token immediately | Reject for customer data and commerce operations |

#### Recommended policy for owner review

1. **Logout current device:** revoke the app's refresh/session credential on the server, then delete local secure-storage entries. If the network is unavailable, clear local credentials immediately and record that server revocation is pending only if a safe retry mechanism exists; do not claim remote revocation succeeded without a server response.
2. **Logout all devices / credential compromise:** revoke all refresh sessions for the account and invalidate the server-side session/security version used by sensitive APIs. Password changes and account recovery should trigger the same policy, subject to the final threat model.
3. **Disable account:** prevent new authentication and refresh immediately; deny sensitive API access using server-checked account/session state. Do not rely solely on access-token expiry if the product requires prompt disablement.
4. **Access-token lifetime:** do not select a numeric duration until deployment, load, and threat-model decisions are recorded. Use a short-lived access token as a starting principle; define the maximum tolerated stale-access window explicitly.
5. **Refresh-token behavior:** if refresh tokens are selected, use rotation and replay/reuse detection when supported by the chosen provider. Persist only the app-issued refresh/session credential in platform-backed secure storage; never put it in logs or URLs.
6. **Availability trade-off:** define fail-closed behavior for sensitive customer data when authoritative session/account state cannot be checked. Do not silently fall back to trusting a token whose account may have been disabled.
7. **Auditability:** log security event type, subject identifier in a privacy-appropriate form, session identifier, timestamp, result, and correlation ID. Never log raw tokens, passwords, codes, or provider secrets.

#### Acceptance tests required before release

- A revoked refresh token cannot mint another access token.
- Reusing a rotated refresh token is detected and follows the documented family/session-revocation policy.
- Logging out one device does not unintentionally log out other devices unless the user selected “log out everywhere.”
- Logging out everywhere and password recovery invalidate all sessions according to the documented contract.
- A disabled account cannot sign in, refresh, or read customer orders using a previously issued access token beyond the explicitly accepted propagation window.
- Session-state store outage follows the documented fail-closed behavior for sensitive endpoints.
- Local logout clears credentials even if remote revocation fails, while UI/telemetry does not falsely claim the server revoked the session.
- No token or secret appears in application logs, analytics, crash reports, URL parameters, or error responses.

**Recommended design direction:** use revocable server-side refresh sessions with rotation/reuse detection, plus server-side account/session eligibility checks on sensitive customer endpoints. The maximum disablement/revocation latency, session persistence schema, and outage policy still require owner approval and deployment-specific verification. Do not implement these defaults without closing the gate.

Official background: Microsoft documents that backend account disablement does not automatically invalidate an already-issued authentication cookie, and that periodic/server-side validation trades latency against database load ([ASP.NET Core cookie authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie)). Its Identity API guidance similarly describes short-lived access tokens, longer-lived refresh tokens, and the security/performance trade-off of session revalidation ([Identity API authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization)).


### Source-level auth boundary verification — 2026-10-20

This subsection records a fresh read of the branch's constitution, mobile package manifest, and current auth screen. It is a repository inspection, not a runtime/device test.

#### Evidence from the current branch

- The constitution at `docs/00-PROJECT-CONSTITUTION.md` requires a Modular Monolith, explicit design decisions, resource-level authorization, and verified acceptance criteria. Its identity direction names ASP.NET Core Identity plus OpenIddict and .NET 10 as a target direction, while requiring a separate compatibility review before a framework/package upgrade.
- `apps/customer-mobile/package.json` pins Expo `~57.0.16` and React Native `0.86.3`. Its current dependencies do **not** include `expo-auth-session`, `expo-crypto`, `expo-web-browser`, or `expo-secure-store`. Therefore, the auth libraries remain candidates only; no dependency adoption is implied by the docs.
- `apps/customer-mobile/app/auth.tsx` is a UI prototype. Google, sign-in, and create-account actions call the same “not connected” alert; the screen explicitly says no credentials are sent and no account is created. This is the correct safe state until the server and session contract exist.

#### Critical trust boundary: provider identity versus Baladna session

The first implementation must not treat a Google access token as a Baladna API access token, nor accept a client-supplied customer ID as proof of ownership.

1. The identity provider authenticates the person; its response is validated according to the selected provider/OIDC contract.
2. The trusted backend validates the provider response and maps the stable provider subject to exactly one Baladna identity account. Email/phone values alone must not trigger silent account linking.
3. The backend provisions or resolves the Baladna customer profile through a trusted, idempotent workflow; an incomplete or ambiguous mapping returns a safe pending/recovery state, not customer data.
4. Baladna issues or establishes its own API session according to the selected OpenIddict/session design. The mobile app uses only that documented session for Baladna APIs.
5. Every protected customer-order request derives the actor from the validated Baladna session and applies ownership in the server-side query. UI route guards and local storage are not authorization boundaries.

#### Recommended next implementation gate (still not authorized)

The next work item should be a **focused compatibility spike**, not a live login button:
- identify the actual .NET SDK/runtime installed in CI and the target/deployment constraint;
- pin a compatible stable package set for the new auth/API host without changing existing commerce project target frameworks in the same change;
- prove clean restore/build and tests in CI;
- add a documented identity-provider callback/redirect contract and a sequence diagram covering provider authentication, Baladna account mapping, customer provisioning, Baladna session issuance, refresh, logout, and disablement;
- define the server-side session persistence boundary and migration/rollback strategy before adding session tables;
- define exact error outcomes for duplicate provider-subject mapping, unverified contact, already-linked account, pending provisioning, provider outage, and session-store outage;
- only after those proofs, implement the smallest server-side vertical slice with tests first; then connect the mobile client and verify Android/iOS development builds.

The official Expo guidance confirms that `expo-auth-session` supports browser-based OAuth/OIDC flows, PKCE, and redirect handling, but also warns that Expo Go cannot validate OAuth/OIDC redirect behavior because its app scheme cannot be customized; use a development build for that proof ([Expo authentication guide](https://docs.expo.dev/guides/authentication/), [AuthSession reference](https://docs.expo.dev/versions/latest/sdk/auth-session/)). Current “latest” docs may describe a newer Expo SDK, so exact package versions must be checked against SDK 57 rather than copied from the latest page.

**Status:** current screen remains intentionally non-functional for authentication. No auth package was installed, no credential was transmitted, and no provider or device flow was tested in this documentation-only step. The design gate remains **NOT READY FOR IMPLEMENTATION** until the open owner decisions and compatibility proof are closed.


### CI/runtime compatibility gap — verified from repository workflow — 2026-10-20

A fresh source check identified a concrete blocker that must be resolved before the proposed .NET 10 authentication host can be built in CI.

#### Verified repository facts

- `.github/workflows/dotnet.yml` explicitly installs `.NET 8.0.x` via `actions/setup-dotnet@v4`.
- The same workflow restores and tests `LocalCommercePlatform.sln` without a .NET 10 SDK setup step.
- `src/LocalCommerce.Infrastructure/LocalCommerce.Infrastructure.csproj` targets `net8.0` and pins `Microsoft.EntityFrameworkCore 8.0.31` plus `Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11`.
- No root `global.json` was found at the expected path during this source check. That is not proof no SDK pin exists elsewhere; the workflow itself is the direct evidence for the CI SDK configuration.
- The repository currently does not have a verified .NET 10 auth host/package matrix. The existing green .NET workflow proves the current .NET 8 solution's configured checks, not that a new .NET 10 project can restore/build/test in CI.

#### Why this is time-sensitive

As of this review, Microsoft's official support policy lists .NET 8 support ending on **November 10, 2026**, while .NET 10 LTS is supported until **November 14, 2028** ([official .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy)). This makes a documented target-framework decision and migration plan time-sensitive; it does not justify an unreviewed solution-wide upgrade.

#### Options and recommendation

| Option | Advantages | Risks / required proof |
|---|---|---|
| Retarget the whole solution and CI to .NET 10 + EF Core 10 + matching Npgsql provider in one focused migration | One consistent supported runtime/provider family; aligns with the constitution's target direction | Broad impact across existing projects, tests, SQL migrations, and deployment; requires full build/test and PostgreSQL migration/recovery proof |
| Keep existing commerce projects on .NET 8 and introduce an isolated .NET 10 auth/API host with its own EF Core 10/Npgsql 10 persistence boundary | Limits immediate changes to the existing commerce code | Adds a project and explicit integration boundary; must prove no shared EF context/provider mixing, define deployment and transaction ownership, and verify all solution projects build under the CI SDK |
| Keep the first auth host on .NET 8 temporarily | Smaller immediate package/runtime change | Very short remaining support window; requires an explicit time-bound exception and a committed upgrade date, so not recommended as the default for a new production auth host |

**Recommendation:** do not choose an option by silently editing the workflow or project files. Prepare a focused runtime migration decision with project-by-project target frameworks, package versions, deployment runtime, migration ownership, rollback strategy, and test evidence. The owner must approve the scope before implementation. If the selected direction includes .NET 10, update CI SDK setup as part of that approved migration and verify the complete solution plus representative PostgreSQL upgrade/restore paths.

#### CI acceptance evidence required for this gap

- CI installs the intended SDK and records `dotnet --info`.
- The complete solution restores, builds, and tests using the intended target-framework/package matrix.
- Any mixed-framework boundary is explicit and does not share EF Core contexts or incompatible provider stacks.
- PostgreSQL schema migration, backup/restore, and recovery are tested against a representative existing database.
- Deployment environment confirms the selected runtime is installed and supported.
- Security tests cover issuer/audience/signature/expiry, account disablement, refresh rotation/reuse, logout, and session-store outage before auth is released.

**Status:** concrete gap identified; no workflow, project target framework, package, or migration was changed in this documentation-only step. The auth design gate remains **NOT READY FOR IMPLEMENTATION**.
