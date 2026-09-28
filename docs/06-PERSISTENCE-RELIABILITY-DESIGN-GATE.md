# Persistence & Reliability Design Gate

Status: DESIGN REVIEW

## Objective

Prevent data corruption, duplicate commercial effects and concurrency bugs before M0 implementation.

## Money

Money must not use floating-point arithmetic.

Initial model:

- amount stored as fixed-precision decimal in persistence, or an equivalent integer minor-unit representation.
- currency is explicit.
- calculations use one consistent rounding policy.
- persisted order totals are snapshots, not recalculated from mutable catalog data.

The exact Money value-object representation is an implementation decision after confirming database/provider constraints.

## Order Snapshot

OrderItem must preserve at least:

- ProductId
- Product/variant display name
- UnitPrice
- Quantity
- LineDiscount where applicable
- LineTotal

Order-level snapshot data must preserve the commercial values needed to reproduce what the customer purchased.

Catalog edits after checkout must not rewrite historical Order data.

## Pricing Authority

Cart prices are provisional.

At Create Order:

1. Load authoritative current catalog data.
2. Validate product/store availability.
3. Recalculate the checkout snapshot on the server.
4. Persist Order and OrderItems atomically.
5. Never trust client-submitted totals.

## Concurrency

M0 must protect at least:

- two simultaneous order attempts against the same cart
- product availability changes during checkout
- duplicate command delivery

Initial strategy:

- database transaction boundaries
- optimistic concurrency/version field where mutable aggregates require it
- unique constraints for invariants that must be database-enforced
- idempotency records for retryable commands

Pessimistic locking is not the default; introduce it only when an identified contention case requires it.

## Idempotency Record

Minimum conceptual fields:

- ActorId
- Operation
- IdempotencyKey
- RequestFingerprint
- Status
- StoredResponse/ResultReference
- CreatedAt
- CompletedAt

Required behavior:

- same actor + operation + key + same fingerprint => return original result
- same key with different fingerprint => conflict
- concurrent requests using the same key must converge to one commercial effect

Retention/cleanup policy remains an operational decision.

## Database Integrity

Use database constraints for facts that must never be violated, including:

- required foreign keys
- unique business identifiers
- valid non-negative quantities
- required money/currency fields
- one active cart per customer/store as applicable to the chosen cart model

Indexes must be derived from actual query patterns, not added indiscriminately.

## Failure Model

Every critical command must define behavior for:

- validation failure
- authorization failure
- concurrency conflict
- database failure
- external provider failure
- client retry

A retry must either safely repeat a non-commercial operation or resolve through idempotency.

## Events and Outbox

M0 does not require Kafka/RabbitMQ.

In-process events may coordinate local side effects.

If a durable external notification/integration must be guaranteed with the same transaction as business state, use an Outbox pattern before relying on an external broker.

This is a design rule, not a commitment to introduce a broker now.

## Recovery

M0 must include:

- migration strategy
- database backup/restore procedure
- failure logging with correlation ID
- audit evidence for critical commands
- smoke test after deployment

Production readiness is not complete until restore behavior is verified, not merely documented.
