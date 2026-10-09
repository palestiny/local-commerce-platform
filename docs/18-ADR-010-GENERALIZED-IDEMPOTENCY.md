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

Persistence implementation is GREEN VERIFIED against real PostgreSQL. The defined M1 Delivery command concurrency scenarios have also passed in CI run `37861612028`; see ADR-009 for the exact command-race coverage and its explicit limits.

## Verification Evidence

CI run `36903751445` (#385), commit `78453abcc7fce887d0e11b60eca2d8d565ba5097`, completed successfully and verified:

- reservation race against real PostgreSQL
- replay/fingerprint persistence semantics
- same-transaction completion
- rollback behavior
- migration/recovery smoke test

## Verification Status

The required PostgreSQL persistence scenarios are GREEN VERIFIED in CI run `36903751445` (reservation race, replay/fingerprint persistence, same-transaction completion, rollback, and migration/recovery smoke test). The subsequent M1 concurrency run `37861612028` also passed the Delivery command contention suite and migration/recovery smoke test.

Future command handlers must still provide command-specific tests for replay, fingerprint conflicts, transactional completion/rollback, and relevant concurrent mutation paths. This ADR does not imply that every future command is automatically verified merely because the shared persistence capability is proven.
