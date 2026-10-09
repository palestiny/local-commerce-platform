# Module Boundaries & Dependency Rules

Status: **DOCUMENTED BASELINE — CHECKLIST RECORDS FOUNDATION REVIEW PASS; CODE-LEVEL CONFORMANCE REQUIRES CONTINUED VERIFICATION**

## Purpose

Define ownership and dependency direction for the Modular Monolith before production code.

## Modules

| Module | Owns | Must not own |
|---|---|---|
| Identity | users, roles, authentication identity | business order rules |
| Merchant | merchant profile and merchant ownership | customer cart/order lifecycle |
| Store | store lifecycle, operating state, zones | product pricing rules |
| Catalog | products, variants, availability, catalog data | order snapshots |
| Cart | active cart and cart items | final order totals |
| Ordering | orders, order items, order lifecycle, order history | driver assignment internals |
| Delivery | deliveries, assignment and delivery lifecycle | commercial order pricing |
| Payments | payment records and payment lifecycle | order fulfillment state |
| Notifications | notification intent and delivery attempts | business state transitions |
| Administration | operational access and configuration | domain ownership of other modules |

## Dependency Direction

Preferred direction:

API -> Application -> Domain
Infrastructure -> Application/Domain abstractions

Rules:

- Domain must not depend on Infrastructure or API.
- Application orchestrates use cases and transactions.
- API maps transport contracts to application requests.
- Infrastructure implements persistence and external providers.
- A module must not directly modify another module's aggregate state.
- Cross-module changes use explicit application commands or domain/application events.
- Shared kernel stays intentionally small: primitives, errors, identifiers, money/time abstractions where genuinely shared.

## Aggregate Ownership

Initial aggregate candidates:

- Store
- Product
- Cart
- Order
- Delivery
- Payment

An aggregate owns its invariants and state transitions. External code requests behavior through its public methods/use cases rather than mutating internal state.

## Transaction Rule

A single database transaction may coordinate multiple aggregates when one business operation requires atomic persistence. This does not make those aggregates one aggregate.

Cross-aggregate coordination must remain explicit.

## Review Requirement

Before implementation, every aggregate must document:

- identity
- owned state
- invariants
- commands
- allowed transitions
- concurrency strategy
- persistence boundary
- emitted events, if any

No module split into microservices is implied by these boundaries.
