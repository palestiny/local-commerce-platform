# ADR-007 — Order vs Delivery State Authority

Status: PROPOSED

## Context

The foundation models Order as the commercial transaction aggregate and Delivery as an independent fulfillment aggregate. However, the current Order lifecycle also contains DRIVER_ASSIGNED, PICKED_UP, OUT_FOR_DELIVERY and DELIVERED. This creates a potential duplicate source of truth.

The decision must be made before implementing either aggregate so state transitions, persistence, APIs, tests and failure handling all use one coherent authority model.

## Options

### Option A — Strict separation

Order owns the commercial lifecycle. Delivery owns assignment, driver acceptance, pickup, transit, delivery completion and delivery failure. The customer-facing order view may expose derived delivery information, but Delivery remains authoritative for fulfillment state.

Advantages:
- One source of truth for fulfillment.
- Clear aggregate ownership.
- Fewer synchronization bugs.
- Delivery can evolve independently from commercial Order state.
- Failure/retry semantics remain localized to Delivery.

Costs:
- Order queries needing delivery progress require a coordinated read or derived view.
- Some customer-facing screens cannot be backed by Order alone.

### Option B — Coordinated projection

Delivery remains authoritative, while Order stores selected fulfillment milestones as a synchronized projection.

Advantages:
- Convenient order-centric reads.
- Simpler denormalized query shape for some screens.

Costs:
- Duplicate state must be synchronized.
- Temporary divergence becomes possible.
- More rules are required for retries, failures and recovery.
- Future code can accidentally treat the projection as authoritative.

## Analysis

The platform already treats Order and Delivery as separate aggregates and modules. The business distinction is also clear:

- Order answers what commercial transaction the customer made and its commercial state.
- Delivery answers where fulfillment is in its operational lifecycle.

Keeping fulfillment state authoritative inside Delivery preserves the boundary already established elsewhere in the architecture. The main cost of strict separation is query composition, not business ambiguity. A coordinated read is preferable to maintaining two mutable sources of truth.

## Proposed Decision

Adopt Option A — Strict separation.

This remains PROPOSED until explicitly accepted.

Resulting model:

Order lifecycle: CREATED -> PENDING_STORE_CONFIRMATION -> ACCEPTED -> PREPARING -> READY_FOR_PICKUP

Commercial exceptions: REJECTED, CANCELLED

Delivery lifecycle: UNASSIGNED -> ASSIGNED -> PICKED_UP -> OUT_FOR_DELIVERY -> DELIVERED

Delivery exception: FAILED

Payment remains independent: PENDING -> PAID / FAILED / REFUNDED

The customer-facing order representation may include derived delivery progress, but the Order aggregate must not own or independently transition Delivery milestones.

## Consequences

1. Order commands stop at commercial/merchant preparation milestones.
2. Delivery commands own driver/pickup/transit milestones.
3. The application layer coordinates Order and Delivery where one business operation spans both.
4. Customer APIs may compose Order + Delivery into one response model.
5. Tests verify that Delivery transitions do not require mutating Order fulfillment state.
6. Failure handling must define what happens when Order and Delivery operations cannot complete together.
7. No duplicate fulfillment status field should be introduced merely for UI convenience without an explicit projection decision.

## Required Follow-up Before M0 Implementation

- Update the Order lifecycle document.
- Update the foundation review checklist.
- Add RED tests for both lifecycles.
- Define application transaction boundaries for READY_FOR_PICKUP, Delivery creation/assignment, pickup and delivery completion.
- Define the customer-facing composed status/read model.
- Define cancellation and delivery-failure semantics.

## Acceptance

This ADR becomes ACCEPTED only after the project owner explicitly approves Option A.