# Testing Strategy

## Goal
Prove business correctness, not only code execution.

## Layers
- Domain: invariants, state machines, value objects and pricing rules.
- Application: commands, orchestration, ownership, idempotency and failure behavior.
- API/Contract: requests, responses, status codes, validation, authorization and errors.
- Integration: PostgreSQL, migrations, transactions, concurrency and idempotency persistence.
- End-to-End Smoke: critical business journey.

## M0 RED Set

Create Order tests must cover at least:
1. Valid cart creates correct Order.
2. Empty Cart is rejected.
3. Unavailable Product is rejected.
4. Inactive Store is rejected.
5. Foreign Cart is rejected.
6. Price snapshot is preserved.
7. Duplicate idempotency key does not create a duplicate Order.
8. Same idempotency key with a different payload is rejected.

Merchant state-transition tests follow before implementing merchant commands.

## Bug Rule

Every discovered defect follows:

reproduce -> isolate -> root cause -> regression test -> fix -> full-flow verification -> document if reusable.

A symptom-only test is insufficient when the underlying invariant is known.
