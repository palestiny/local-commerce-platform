# Foundation Review Checklist

Status: **APPLICATION GREEN — PERSISTENCE NEXT**

## Review Scope

| Area | Status | Evidence |
|---|---|---|
| Product/domain model | PASS | 02-DOMAIN-ARCHITECTURE-DESIGN-GATE.md |
| Module boundaries | PASS | 05-MODULE-BOUNDARIES.md |
| Order lifecycle | PASS | 03-ORDER-LIFECYCLE.md + ADR-007 |
| Payment lifecycle | PASS | Separate Payment lifecycle defined |
| Persistence model | PASS WITH OPEN IMPLEMENTATION DETAILS | 06-PERSISTENCE-RELIABILITY-DESIGN-GATE.md |
| Idempotency | PASS WITH IMPLEMENTATION DETAIL OPEN | 06-PERSISTENCE-RELIABILITY-DESIGN-GATE.md |
| API direction | PASS WITH CONTRACTS OPEN | 07-API-SECURITY-DESIGN-GATE.md |
| Authorization | PASS | Resource-level ownership rules defined |
| Security baseline | PASS WITH PROVIDER OPEN | 07-API-SECURITY-DESIGN-GATE.md |
| Testing strategy | PASS | 04-TESTING-STRATEGY.md |
| Pilot/product decisions | OPEN BY DESIGN | 01-PROJECT-FOUNDATION.md / 14-OPEN-QUESTIONS.md |

## Resolved Blocking Decision

### Order vs Delivery state authority

ADR-007 is ACCEPTED with **Option A — Strict separation**.

- Order owns commercial state through READY_FOR_PICKUP.
- Delivery owns assignment and fulfillment state.
- Delivery is the sole mutable source of truth for driver/pickup/transit/completion state.
- Customer-facing order views may compose Order + Delivery without duplicating mutable fulfillment state.

This removes the previous overlap between Order and Delivery.

## Foundation Exit Criteria

- One state authority is documented: PASS.
- Lifecycle transition tables are consistent: PASS.
- RED tests are the next verification artifact: READY.
- Aggregate transaction/concurrency rules are implementable: PASS WITH M0 IMPLEMENTATION DETAIL OPEN.
- API contract can be derived without ambiguous state ownership: PASS.
- No production feature code depends on unresolved domain semantics: PASS.

## Executable Foundation Evidence

### Domain GREEN

- Domain invariants and commercial lifecycle are implemented.
- CI verified the Domain test suite with **8 passed**.

### Application GREEN

- Create Order orchestration is implemented.
- CI verified the Application suite with **12 passed**.
- Full solution CI verified **20 passed, 0 warnings, 0 errors** on commit `8b9bc4662d113671cf9c43b389ce36d65106237d`.

## Remaining M0 Work

These are now implementation/integration concerns, not unresolved foundation semantics:
1. PostgreSQL persistence model and mappings.
2. Real transaction boundary covering Order, OrderItems, Cart consumption, and idempotency completion.
3. Database-enforced idempotency uniqueness and concurrent same-key convergence.
4. Cart-consumption concurrency behavior.
5. Persistence integration tests.
6. Concrete migration/backup/restore operational path.
7. Delivery creation/assignment semantics.
8. Customer-facing composed status/read model.
9. Cancellation and Delivery FAILED semantics.

These must be resolved in the relevant M0 design/test artifacts before their implementation.

## Next Step

Complete the Persistence & Reliability Design Gate before introducing HTTP/API behavior.
