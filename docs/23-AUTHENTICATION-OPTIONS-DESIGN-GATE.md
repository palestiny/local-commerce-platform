# Authentication Options — Design Gate

Status: **ANALYSIS / TRADE-OFFS — OWNER DECISION REQUIRED**

## Purpose

Compare realistic authentication approaches for the Local Commerce Platform before selecting a provider or implementing the HTTP adapter. This is a decision aid, not an accepted ADR and not authorization to implement authentication.

## Requirements that do not depend on provider

Whichever option is selected, the platform must:
- Derive actor identity from a verified server-side authentication context, never from a client-supplied CustomerId/MerchantId/DriverId.
- Enforce resource-level authorization in application/use-case boundaries.
- Define account verification, recovery, expiry, revocation, session/device handling, and privileged-account protection.
- Avoid storing plaintext passwords or inventing a home-grown password protocol.
- Keep provider-specific claims and SDKs out of Domain and core Application code.
- Support distinct Customer, Merchant, Driver, and Administrator capabilities without assuming a role claim alone grants access to every resource.
- Provide testable unauthenticated, invalid/expired credential, forbidden, and resource-concealment behavior.
- Avoid putting secrets, bearer tokens, or raw credentials in logs.
- Support Arabic and English user experience; localization is an application concern and must not be delegated to an auth vendor assumption.

## Options

### Option A — ASP.NET Core Identity with first-party authentication endpoints

**Shape:** Store user/account records in the platform's PostgreSQL database; use ASP.NET Core Identity for password hashing, account management, and security primitives. Select and design the session/token mechanism separately; do not assume that adding Identity automatically settles mobile token issuance or refresh-token rotation.

**Advantages**
- Direct control over account lifecycle and platform-specific workflows.
- Fits the existing ASP.NET Core/.NET modular monolith and PostgreSQL baseline.
- Reduces dependence on a third-party identity service for core account data.
- Can be operated without a per-month managed-auth vendor bill, though hosting, email/SMS, security operations, and maintenance still cost money.

**Costs / risks**
- The project owns endpoint hardening, verification and recovery flows, abuse protection, session/token lifecycle, revocation, operational monitoring, and security maintenance.
- Easy to underestimate work around refresh tokens, multi-device sessions, account recovery, rate limits, credential stuffing, and privileged access.
- Mobile/web client needs affect the appropriate session/token architecture.
- Email/SMS delivery is a separate provider and reliability decision.

**Best fit when:** Control, platform-owned identity data, and integration flexibility outweigh the additional security engineering and operating responsibility.

### Option B — Managed authentication service

**Shape:** A provider handles credential authentication and some account/session lifecycle; the platform validates provider-issued credentials and maps the authenticated subject to its own internal user and authorization model.

Examples to evaluate, not preselected: Auth0, Clerk, and Supabase Auth.

**Advantages**
- Can reduce the amount of authentication protocol and account-recovery code the project must operate.
- May provide hosted sign-in, verification, social login, and session tooling, depending on plan/provider.
- Can shorten the initial authentication implementation if the chosen provider fits web/mobile needs.

**Costs / risks**
- Pricing, free-tier limits, feature availability, regional hosting/data processing, exportability, and contractual terms vary and must be checked against current official provider documentation before selection.
- Vendor outage or account/tenant misconfiguration can affect sign-in.
- Migration and identity portability require deliberate subject mapping and account-linking rules.
- Provider SDKs and claims must not become the domain authorization model.
- Merchant, Driver, and Administrator authorization still belongs to this application.

**Best fit when:** Faster delivery and lower initial protocol-operation burden are more important than minimizing external identity dependency, and current pricing/terms pass the project's constraints.

### Option C — External identity platform with federation / enterprise-oriented setup

**Shape:** Integrate with an established identity platform using standard OIDC/OAuth flows; the platform may be self-managed or managed by an organization/provider.

**Advantages**
- Standard protocols and possible federation with external organizations.
- Useful if enterprise SSO, partner identity, or more complex organizational identity becomes a real requirement.

**Costs / risks**
- Likely more operational/configuration complexity than the initial pilot needs.
- Federation, tenant boundaries, and role/claim mapping can expand the threat model.
- Does not automatically solve consumer account recovery, merchant onboarding, driver identity checks, or application-level resource ownership.

**Best fit when:** A verified enterprise/partner federation requirement exists. No such requirement is currently established for the first pilot.

## Comparative assessment

| Criterion | Option A: ASP.NET Identity | Option B: Managed auth | Option C: External/federated identity |
|---|---|---|---|
| Initial implementation effort | Medium–high | Low–medium if provider fits | Medium–high |
| Direct control of account data/flows | High | Provider-dependent | Provider-dependent |
| Ongoing security operations owned by project | High | Lower for provider-managed protocol pieces; still non-zero | Medium–high, depending on hosting |
| Vendor dependency | Low for identity protocol/service | High | Medium–high |
| Pricing certainty today | Hosting/operations still unknown | NOT PROVEN; current plans must be checked | NOT PROVEN |
| Mobile/web fit | Depends on designed session/token flow | Must verify selected provider's current SDK/flow support | Depends on provider and standards |
| Resource-level authorization | Must be implemented by this platform | Must be implemented by this platform | Must be implemented by this platform |
| Pilot fit | Viable if security work is properly scoped | Viable if cost, terms, and platform fit are verified | No demonstrated need yet |

Ratings are qualitative architectural assessments, not measured implementation estimates or provider feature guarantees.

## Recommendation — NOT AN OWNER DECISION

**Recommended shortlist: Option A versus Option B; defer Option C unless a concrete federation requirement emerges.**

Given the existing ASP.NET Core + PostgreSQL modular-monolith baseline, Option A is the lowest-vendor-dependency path. However, it transfers more security and account-lifecycle work to the project. A managed provider may be the better pilot choice if its current free/paid limits, Egyptian user experience, web/mobile support, data handling terms, and exit path fit the actual budget and deployment plan.

Do not choose between A and B solely on the existence of a free tier or on assumed prices. Those are current facts to verify only after a shortlist and the deployment/budget constraints are known.

## Decision criteria to close before selection

The owner should compare:
1. Expected pilot account volume and sign-in frequency.
2. Whether first launch is web-only or includes native mobile.
3. Budget and acceptable recurring vendor spend.
4. Email/phone verification and recovery requirements.
5. Social login requirements, if any.
6. Account lifecycle and admin/merchant onboarding requirements.
7. Hosting/data processing and legal review requirements.
8. Export/migration and provider-outage plan.
9. Who will operate security alerts, abuse prevention, recovery, and account support.

## Non-negotiable architecture boundary

Introduce a provider-neutral application boundary for the authenticated actor and keep resource authorization in the platform. The exact interface and token validation arrangement must be designed against the selected option; do not create a speculative abstraction with no caller or implement provider SDKs before the decision.

## Acceptance criteria for this gate

- Owner selects Option A, Option B with a named provider, or explicitly defers the choice with a blocking condition.
- For managed providers, current official pricing/limits, data-processing terms, regional/data handling, and relevant client support are verified and cited in the final decision record.
- Session/token lifecycle, revocation, recovery, verification, and abuse controls are documented.
- The authentication-to-application actor boundary and resource authorization policy are testable.
- API error semantics and required contract/security tests are agreed.
- Only then may the implementation design proceed; HTTP controllers remain blocked until the wider API contract/security gate closes.

## Current status

**OPEN.** No provider is selected, no price is assumed, and no authentication implementation is authorized by this document.
