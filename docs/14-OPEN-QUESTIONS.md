# Open Questions

Nothing in this file is considered decided until it is recorded in the Decision Log with rationale and the appropriate status. Items marked **PARTIALLY RESOLVED** have an owner-confirmed direction but still contain unresolved operational details.

## Owner-confirmed context — not open

- Country: Egypt.
- Initial geographic focus: Dekernes, Dakahlia.
- Languages: Arabic and English.
- Currency: EGP.
- Product positioning: local product discovery across multiple stores, with a simple merchant experience to update products and availability.
- Delivery direction: hybrid.
- Long-term ambition: broad merchant-category coverage, without deciding the initial pilot categories.

## Product / Pilot — Phase B blockers

- [ ] **Pilot wedge:** choose the first merchant categories for a deliberately small pilot.
- [ ] Pilot merchant count / initial cohort size.
- [ ] Exact operating zones and customer delivery-radius rules.
- [ ] Merchant onboarding and verification model.
- [ ] Hybrid delivery operating split: driver engagement and payment responsibility, dispatch/assignment ownership, and merchant/platform/driver duties.
- [ ] Cash-on-delivery collection, custody, reconciliation, and settlement flow.
- [ ] Commercial fee configuration: supported fee types, who pays each fee, calculation basis, values/ranges, discounts, collection, settlement, and merchant-facing disclosure.
- [ ] Merchant SLA and operating expectations for catalog availability and order response.
- [ ] Cancellation, failed-delivery, refund/return, and dispute policies.

**Gate:** Do not begin Phase B implementation until the pilot wedge and its blocking decisions are explicitly approved.

## Experience

- [ ] Brand and product name.
- [ ] Customer surface strategy: web, mobile, or staged combination.
- [ ] Merchant catalog/order-management surface.
- [ ] Driver operational surface.
- [ ] Notification channels, provider, templates, consent/opt-out expectations, and failure handling.

## Technical and Security

- [ ] Authentication approach and provider; session/token lifecycle, recovery, and account verification.
- [ ] Resource-level role/permission matrix for Customer, Merchant, Driver, and Administrator.
- [ ] API contract: first HTTP slice, DTOs, status/error mapping, idempotency header semantics, rate/body limits, timeout budgets, and correlation/trace behavior.
- [ ] Mobile stack.
- [ ] Hosting/deployment target and budget constraints.
- [ ] Object/image storage provider, access policy, upload limits, and retention.
- [ ] Maps/geocoding provider and whether maps are required in the initial pilot.
- [ ] Future online-payment provider and its separate Design Gate.
- [ ] Search evolution beyond PostgreSQL only when measured requirements justify it.
- [ ] Backup/restore, operational monitoring, and incident-response expectations appropriate to the pilot.

## Legal, Commercial, and Governance

- [ ] License recommendation and owner approval before applying a repository license.
- [ ] Legal review/counsel and applicable Egyptian obligations for commerce, privacy/data handling, consumer terms, and driver/merchant arrangements; do not treat this checklist as legal advice or as a compliance conclusion.
- [ ] Privacy policy, terms, merchant agreement, driver terms, and customer-facing cancellation/refund disclosures.
- [ ] Whether any external service terms or data-processing agreements are required.

## Pilot Operations and Measurement

- [ ] Support workflow and escalation owner.
- [ ] Dispute handling and evidence retention.
- [ ] Driver SLA and exception handling.
- [ ] Pilot success measures and numeric thresholds, reviewed and approved by the owner.
- [ ] Pilot duration, cohort, daily operating cadence, and evidence collection plan.

## Decisions that must remain provisional until explicitly resolved

- ADR-005 records Cash on Delivery initially as **PROVISIONAL**. Do not change it to ACCEPTED without an explicit decision record.
- Hybrid delivery is a confirmed direction, not a complete operating model.
- Configurable fees are a requirement direction, not a fee schedule or revenue model.
- Success measures are candidate measures, not approved thresholds.
- Broad category coverage is a long-term ambition, not the initial pilot wedge.

## API implementation block

No HTTP controllers or production endpoints should be implemented until the API contract/security gate is closed, including authentication integration boundary, resource-level authorization, DTOs, idempotency key semantics, abuse controls, HTTP error mapping, and the required contract/security tests.
