# ADR-010 — Generalized Command Idempotency Persistence

Status: ACCEPTED

## Context

Create Order already has PostgreSQL-backed idempotency persistence, while Delivery commands currently depend on application-level idempotency abstractions and test doubles. PostgreSQL command concurrency cannot be considered complete until the idempotency reservation/completion boundary is proven under the same transaction model.

The existing M0 record is coupled to Order through fields such as OrderId and OrderNumber. Reusing it directly for Delivery would couple a cross-cutting reliability concern to one aggregate.

## Options

### Option A — Generalized idempotency record

Introduce a resource-neutral idempotency record containing actor/tenant scope as required, operation, key, request fingerprint, lifecycle status, resource type/resource id, and timestamps.

Advantages:
- One persistence mechanism for Order, Delivery, Payment, and future commands.
- Consistent uniqueness and replay semantics.
- Reliability concern remains independent from business aggregates.

Costs:
- Requires evolving the current Create Order persistence/schema and migration path.
- Requires carefully preserving existing replay and conflict semantics.

### Option B — Delivery-specific idempotency table

Keep Create Order's existing record unchanged and add a Delivery-specific table.

Advantages:
- Smaller immediate change.
- Lower migration scope for M0.

Costs:
- Duplicated persistence semantics.
- Future Payment and other commands may repeat the same pattern.
- Cross-cutting reliability behavior can drift between modules.

## Decision

Adopt Option A — Generalized idempotency record.

The project owner explicitly approved this direction. The idempotency persistence model is a cross-cutting reliability capability and must remain resource-neutral rather than being coupled to Order.

## Consequences

Persistence implementation is now GREEN VERIFIED against real PostgreSQL. Delivery command concurrency remains open because the application handlers and coordinated cross-aggregate operations still require end-to-end contention tests.

## Verification Evidence

CI run `36903751445` (#385), commit `78453abcc7fce887d0e11b60eca2d8d565ba5097`, completed successfully and verified:

- reservation race against real PostgreSQL
- replay/fingerprint persistence semantics
- same-transaction completion
- rollback behavior
- migration/recovery smoke test

## Remaining Verification Required

- RED tests for reserve/replay/fingerprint conflict.
- Unique-key race against real PostgreSQL.
- Same-transaction completion behavior.
- Rollback behavior.
- Concurrent command behavior where only one mutation commits.
- Migration/recovery smoke test.
