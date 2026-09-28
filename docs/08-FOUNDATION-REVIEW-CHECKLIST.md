# Foundation Review Checklist

Status: BLOCKED — one domain authority decision remains

## Review Scope

| Area | Status | Evidence |
|---|---|---|
| Product/domain model | PASS | 02-DOMAIN-ARCHITECTURE-DESIGN-GATE.md |
| Module boundaries | PASS | 05-MODULE-BOUNDARIES.md |
| Order lifecycle | BLOCKED | Order and Delivery currently overlap on fulfillment milestones |
| Payment lifecycle | PASS | Separate Payment lifecycle defined |
| Persistence model | PASS WITH OPEN IMPLEMENTATION DETAILS | 06-PERSISTENCE-RELIABILITY-DESIGN-GATE.md |
| Idempotency | PASS WITH IMPLEMENTATION DETAIL OPEN | 06-PERSISTENCE-RELIABILITY-DESIGN-GATE.md |
| API direction | PASS WITH CONTRACTS OPEN | 07-API-SECURITY-DESIGN-GATE.md |
| Authorization | PASS | Resource-level ownership rules defined |
| Security baseline | PASS WITH PROVIDER OPEN | 07-API-SECURITY-DESIGN-GATE.md |
| Testing strategy | PASS | 04-TESTING-STRATEGY.md |
| Pilot/product decisions | OPEN BY DESIGN | 01-PROJECT-FOUNDATION.md / 14-OPEN-QUESTIONS.md |

## Blocking Decision

### Order vs Delivery state authority

Current documentation defines Delivery as an independent aggregate, but also places fulfillment milestones inside Order:

DRIVER_ASSIGNED -> PICKED_UP -> OUT_FOR_DELIVERY

This must be resolved before implementing either aggregate.

### Option A — Strict separation

Order owns commercial state.
Delivery owns assignment and fulfillment state.
The application may derive an order-facing fulfillment view from Delivery, but Delivery remains the source of truth for driver/pickup/transit state.

Benefit: strongest separation and least duplicated state.
Cost: order queries need a coordinated/derived fulfillment view.

### Option B — Coordinated projection

Delivery remains authoritative for fulfillment, while Order stores selected fulfillment milestones as a synchronized projection.

Benefit: simpler order-centric reads.
Cost: introduces synchronization rules and a risk of stale/contradictory duplicated state.

### Decision rule

Do not select an option because it is convenient for the first implementation. Select it based on the source-of-truth model and the failure behavior we want the platform to guarantee.

Until this decision is documented, the Foundation Gate remains BLOCKED.

## Exit Criteria

The gate can move to READY only when:
- one state authority is documented
- lifecycle transition tables are updated consistently
- corresponding RED tests are defined
- aggregate transaction/concurrency rules are implementable
- API contract can be derived without ambiguous state ownership
- no production feature code depends on unresolved domain semantics

## Next Step

Resolve the single blocking state-authority decision, update the lifecycle/design documents, then begin TDD RED for Create Order.