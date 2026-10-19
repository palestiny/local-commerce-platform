# بلدنا — Local Service Request Pilot Plan

**Status:** Proposed plan based on the owner's latest direction; not yet a final implementation authorization  
**Repository:** `palestiny/local-commerce-platform`  
**Working branch:** `design-reference-import`  
**Market starting point:** Dekernes / Dakahlia, Egypt (existing project context; exact pilot coverage remains open)  
**Owner direction recorded for this proposal:** Real pilot with citizens and service providers; commerce and services remain in the long-term vision; the first end-to-end journey is a local service request; operations are initially assisted manually by the platform team, with each step recorded.

## 1. Executive recommendation

Deliver a **small, controlled local-service-request pilot**, not a broad public launch and not a complete multi-category super-app.

The first pilot should let a citizen:
1. discover a small set of real local service providers;
2. open a provider/service profile and understand the service area and how the request works;
3. submit a service request with only the information needed for that service;
4. receive a request reference and understandable status;
5. get a response from the provider, including acceptance, a clarification request, a proposed time, a quote, or rejection as appropriate;
6. see the final outcome or cancellation and provide feedback.

A platform operator may help onboard providers, check request completeness, contact the provider, update allowed workflow statuses, and resolve operational issues. Manual assistance must be visible, permission-controlled, and recorded in an audit trail. It must not silently impersonate a provider or invent a provider response.

**Do not force this journey into the existing commerce Order model.** A local service request may lead to a quote, appointment, scheduled visit, or simple contact; not every service has a cart, fixed price, delivery, or immediate booking. Define a separate service-request domain/API contract unless domain analysis proves a safe shared abstraction. Reuse cross-cutting capabilities only when the semantics genuinely match.

## 2. Facts verified from GitHub

These are repository observations, not assumptions about production readiness:

- `README.md` describes the project as a local-first commerce platform and identifies a modular monolith, ASP.NET Core/.NET, PostgreSQL, and React + TypeScript as the initial architecture. It says the project is in Foundation / Architecture.
- `docs/00-PROJECT-CONSTITUTION.md` requires a design gate before major features, clear domain boundaries, authorization, audit history, layered testing, and evidence before declaring work complete.
- `docs/01-PROJECT-FOUNDATION.md` and `docs/12-ROADMAP.md` currently center the initial MVP/roadmap on commerce orders and delivery, rather than a service-request journey.
- `docs/14-OPEN-QUESTIONS.md` leaves pilot zones, provider/merchant onboarding, operating model, support, and related operations unresolved.
- The branch `design-reference-import` contains HTML design references including `booking-screen.html`, `seller-profile.html`, `details-screen.html`, `search-screen.html`, `categories-screen.html`, `add-business.html`, and `business-dashboard.html`, as well as commerce-focused screens. These are reference prototypes, not proof that corresponding React screens, backend endpoints, production data, or integrations exist.
- The proposal `docs/29-BALADNA-PRODUCT-VISION-AND-SERVICE-ECOSYSTEM-PROPOSAL.md` explicitly treats the broad service ecosystem as a long-term direction and calls for phased delivery.
- The OpenAPI draft at `docs/28-CUSTOMER-ORDER-OPENAPI-DRAFT.yaml` on branch `docs/api-security-contract-closure-checklist` labels itself a draft for mock/frontend integration; it defines customer Order routes and says they are not production HTTP endpoints. It does not establish a service-request API contract or select an authentication provider.

**Not verified by these facts:** deployed application readiness, current test status, production authentication, availability of a working API, live provider participation, demand in Dekernes, legal/operational permissions for each service category, or a delivery/support team.

## 3. Planning assumptions — must be validated

1. The first pilot will cover one limited and explicitly named area; “Dekernes / Dakahlia” is the starting context, not a finalized coverage map.
2. The pilot will start with **one service category** where providers can respond reliably and requests can be completed safely. The category is intentionally not chosen in this document.
3. Target a small cohort of approximately **5–10 participating providers** and **20–30 invited citizens** for the first usability/operations round. These are planning targets, not market evidence.
4. The pilot can initially run without integrated online payments, dispatch, or guaranteed instant booking. Payment/commission collection is not part of this proposal unless a separate decision gate approves it.
5. Platform staff can perform bounded manual operations during business hours and record actions. Support coverage and response-time promises must be defined before inviting users.
6. The working estimate is **8–12 weeks** from scope freeze to an initial field evaluation, assuming regular development time, accessible owner decisions, provider participation, and no major architectural blockers. This estimate must be revised after code/test/deployment audit and a resource-availability check.

## 4. Proposed pilot scope

### In scope
- One defined pilot area and one chosen service category.
- Small set of real providers with permission to publish their details.
- Provider/service listing, search or category discovery, and profile/detail page.
- Service request form with category-specific fields, optional notes, and contact/availability preferences where appropriate.
- Request reference, status history, and customer-facing updates.
- Provider response recorded as accept, decline, request for information, proposed time, or quote where applicable.
- Simple provider-facing request inbox or a secure assisted workflow approved for the pilot.
- Minimal operations/admin queue for onboarding, request follow-up, complaints, and recovery.
- Audit trail for status changes and staff interventions.
- Clear contact/support path, privacy notice, cancellation/withdrawal behavior, and pilot limitations.
- Arabic RTL experience as a launch requirement; English support is part of the broader project context but exact pilot-language acceptance should be confirmed.

### Explicitly out of scope unless separately approved
- All service categories launching together.
- Universal instant booking or fixed pricing for every service.
- Online payments, wallet, escrow, or commissions.
- Complex in-app chat, voice/video calls, live tracking, AI recommendations, loyalty, and advanced analytics.
- Claims that the platform is an official government/medical/financial service.
- Collecting sensitive health, identity, or financial information that is not strictly needed for the chosen category.
- Automatic provider verification or quality guarantees that the operations process cannot substantiate.

## 5. End-to-end journey and manual operations

1. **Provider recruitment:** obtain consent, verify basic contact/business details proportionate to the category, agree on coverage, working hours, response expectations, and pilot rules.
2. **Publish listing:** operator reviews submitted content and marks it as draft/active/suspended with a reason; record who made the change.
3. **Citizen discovery:** show only active pilot providers; make location/coverage and any known price/availability limitations clear.
4. **Request submission:** validate required fields, avoid requesting unnecessary sensitive data, create a unique reference, and confirm receipt.
5. **Operations triage:** check completeness and route the request to the intended provider. Staff assistance is recorded as a staff action, not a provider action.
6. **Provider response:** record the provider's actual response and timestamp; if contact happened outside the platform, record the channel and operator while avoiding unnecessary conversation content.
7. **Citizen update:** communicate the response and next step in plain language. Do not call a request “booked” or “confirmed” until the provider actually confirms it.
8. **Completion / cancellation / expiry:** record the outcome, reason where known, and actor; permit correction/recovery under defined rules.
9. **Feedback and support:** ask whether the request was handled and capture complaints. Reviews should be limited to verified interactions or clearly labeled pilot feedback.
10. **Daily reconciliation:** compare submitted requests with provider responses and terminal outcomes; follow up on unhandled requests and record incidents.

### Proposed request lifecycle (to be validated in a domain design gate)
`Submitted -> UnderReview (optional) -> SentToProvider -> ProviderResponded -> Accepted / Declined / NeedsInformation / QuoteProvided / TimeProposed -> Completed / Cancelled / Expired`

Do not treat all branches as a single linear state machine. Define valid transitions, actor permissions, timeouts, re-open/retry behavior, and whether a quote or proposed time requires citizen confirmation before the request becomes accepted.

## 6. Success criteria

Targets below are proposed pilot gates. They are not guarantees or validated market benchmarks.

### Gate A — before inviting real users (must-pass)
- The selected category, area, provider cohort, operating hours, support owner, and escalation path are documented.
- The complete happy path and important failure paths pass tests.
- Authentication and server-side authorization prevent customers from reading/updating another customer's requests and providers from accessing requests not assigned to them.
- Staff interventions are role-restricted and auditable.
- Request submission is protected against accidental duplicate creation or duplicate handling.
- Privacy notice, data minimization, retention/access policy, provider consent, and complaint handling are defined for the selected category.
- No open critical/high-severity security or data-integrity defect relevant to the pilot.
- A deployment, backup/recovery, monitoring, and incident runbook exists and has been exercised at least once.
- At least five providers have confirmed participation and their listings have been reviewed before cohort launch.

### Gate B — first field evaluation (initial target)
- 20–30 citizens are invited; report actual participants separately from invitations.
- At least 10 legitimate service requests are submitted, if the recruitment and pilot window make that feasible.
- At least 80% of observed test participants can submit a valid request without direct coaching from the development team.
- At least 80% of legitimate requests receive a recorded provider response within the response window agreed with participating providers. Set the exact window before launch; do not invent an SLA after seeing results.
- 100% of requests have a traceable status history and a recorded terminal outcome or an explicit still-open reason at the evaluation cutoff.
- Zero lost requests and zero unauthorized cross-user/provider access in test evidence.
- Capture cancellation, no-response, incomplete request, provider unavailable, duplicate submit, staff correction, and complaint scenarios.
- Collect citizen usefulness feedback and provider willingness-to-continue; report counts and quotes with consent, not just a blended score.

### Gate C — decision to expand (requires evidence, not just a green build)
- Review the funnel: discovery -> profile viewed -> request started -> request submitted -> provider response -> accepted/fulfilled.
- Report median and range for provider response time, fulfillment/completion rate, cancellation/decline/no-response rates, support effort per request, and repeat intent.
- At least a majority of participating citizens report that the pilot solved a real need, and a majority of providers express willingness to continue; treat this as a directional signal with the small sample size clearly stated.
- No unresolved severe trust, safety, privacy, or operational issue.
- Owner explicitly decides whether to continue, change the category/area/workflow, or pause. Expansion is not automatic.

## 7. Measurement and evidence

Maintain a lightweight pilot register with one row per request and controlled access. Suggested fields: request reference, category, provider ID, creation time, first response time, status transitions, outcome, cancellation/decline/no-response reason, operator actions, support incident ID, and optional feedback. Avoid copying sensitive free-text or personal data into analytics if not needed.

Publish a weekly summary with:
- cohort size and active providers;
- funnel counts and conversion rates with denominators;
- response/fulfillment timing;
- unresolved and overdue requests;
- complaints and incidents;
- manual operations time per request;
- qualitative feedback and limitations;
- decision needed from the owner.

Do not interpret a tiny pilot cohort as statistically representative of the city or as proof of product-market fit.

## 8. Proposed 8–12 week sequence

| Period | Goal | Exit evidence |
|---|---|---|
| Weeks 1–2 | Audit code and designs; choose category/area; interview a small number of citizens/providers; close the Service Request Design Gate | Scope, workflow, data map, provider recruitment plan, acceptance criteria |
| Weeks 3–4 | Define service-request domain/API contract, authorization matrix, operational workflow, and clickable/frontend prototype mapping | Reviewed contract and traceability from screen -> use case -> API -> test |
| Weeks 5–7 | Implement the smallest vertical slice using TDD; add provider/operations assisted handling and audit history | Automated tests plus integrated end-to-end evidence in a non-production environment |
| Week 8 | Security, privacy, reliability, deployment, recovery, and support readiness review | Gate A decision: GO / GAPS / NO-GO |
| Weeks 9–10 | Small controlled field round and daily follow-up | Request register, incident log, participant feedback |
| Weeks 11–12 | Fix critical issues, evaluate outcomes, make expansion decision | Pilot evaluation and owner decision |

This is a planning model, not a promise. If the audit reveals missing foundations or the provider cohort is not ready, keep the gate closed and revise the dates.

## 9. Required GitHub verification before implementation

The following remain **NOT PROVEN by this plan** and must be inspected on the actual target branch before estimating implementation work:
- current solution/project tree and whether the required web/API projects are present and buildable;
- current service/provider/request domain models, application use cases, persistence, migrations, and authorization boundaries;
- test inventory, most recent CI runs, and actual pass/fail state;
- frontend app entry points and whether the HTML references have been imported into the React + TypeScript implementation;
- current API/security design gates, OpenAPI drafts, authentication decision, and route implementation status;
- deployment, environment configuration, logging, monitoring, backups, and runbooks;
- branch/PR status and whether this proposal can be reconciled with the existing commerce roadmap without silently replacing accepted decisions.

Do not claim that tests pass or that the pilot is launch-ready until command/CI evidence is linked in the milestone record.

## 10. Design Gate questions that must close

1. Which single service category and exact pilot area?
2. What constitutes a valid service request for that category, and what minimum fields are required?
3. Is the initial provider response a quote, proposed appointment, acceptance, or simply a contact response?
4. Who is the accountable pilot operator and what response/support hours can be promised?
5. Which provider identity/credential checks are appropriate and feasible?
6. What is the request timeout, cancellation rule, and process for provider non-response?
7. Which channels are permitted for notifications and external contact?
8. What data is retained, for how long, and who can view/export it?
9. What does the citizen pay, if anything, during the pilot? Default proposal: no in-app payment until a separate gate.
10. How does this service pilot coexist with the currently documented commerce-first MVP and roadmap?

## 11. Governance and next action

This document records the owner's chosen direction for planning, but does not silently amend `docs/01-PROJECT-FOUNDATION.md` or `docs/12-ROADMAP.md`, and does not authorize production endpoints, an authentication-provider choice, a deployment, or a public launch.

**Recommended next action:** run a focused repository and design audit, then prepare a **Service Request Design Gate** with a screen-to-domain-to-API-to-test traceability table and explicit trade-offs. Once the owner approves the category, area, operating model, and contract, implement one vertical slice with tests and evidence. Keep the existing commerce roadmap visible until an explicit decision reconciles the two.


## 12. Updated location and recommended first service category (2026-10-16)

### Owner-confirmed location
- **Dekernes, Dakahlia, Egypt** is the pilot location.
- Exact neighborhoods/service radius and whether nearby villages are included remain open.

### Recommendation — proposed, not yet owner-approved
Use **home maintenance requests** as the candidate first category, initially limited to a small number of request types such as plumbing, electrical repair, and home-appliance repair. Do not activate all three automatically: confirm that enough suitable providers will participate and agree to the pilot workflow first.

Why this is the leading candidate:
- It fits a request-and-response workflow without requiring a product catalog, stock, delivery dispatch, or online payment.
- A provider can respond with availability, a clarification question, a visit/inspection fee, or a quote; the platform can record the response while the operator assists manually.
- Public business-search results returned examples categorized as appliance repair, electrical, and plumbing around Dekernes. These listings are **leads only**: the platform has not contacted or verified these providers, current availability, service quality, coverage, or willingness to join.
- Public competitor examples in Egypt use home repair/maintenance as a service-marketplace category, which supports category plausibility but does **not** prove local demand or the viability of this specific pilot.

### Before locking this category
Conduct a short validation round before implementation scope is frozen:
1. Speak with at least 8–10 residents in Dekernes about the last time they needed a plumber, electrician, or appliance repair; ask how they found help, what went wrong, and what evidence would make them trust a new platform.
2. Contact at least 8–10 candidate providers across the shortlisted trades; confirm they serve the pilot area, are willing to respond to platform requests, what information they need, and what response time they can realistically offer.
3. Ask a small number of providers to walk through a sample request and response; do not publish their information or imply partnership without consent.
4. Compare the candidate trades using provider readiness, resident-reported frequency/urgency, ability to complete a request safely, and operational burden.
5. Select one initial trade, or at most a tightly bounded set if provider readiness and request fields are genuinely similar. Record the decision and rationale in the decision log.

### Revised planning status
- Location: **confirmed by owner**.
- Service category: **home maintenance is the current recommendation; validation and owner decision pending**.
- Timing: keep the 8–12 week estimate provisional until the validation, code audit, team availability, and deployment gaps are understood.
