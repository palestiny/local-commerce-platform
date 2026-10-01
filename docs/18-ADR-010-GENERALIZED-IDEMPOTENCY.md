# ADR-010 — Generalized Command Idempotency Persistence

Status: PROPOSED

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

No final decision is recorded by this ADR yet. The project owner must explicitly approve the direction before schema/code changes are made.

## Consequences

Until accepted, Delivery command idempotency remains an open concurrency gate item. No claim of full PostgreSQL command-concurrency GREEN should be made.

## Verification Required After Decision

- RED tests for reserve/replay/fingerprint conflict.
- Unique-key race against real PostgreSQL.
- Same-transaction completion behavior.
- Rollback behavior.
- Concurrent command behavior where only one mutation commits.
- Migration/recovery smoke test.
