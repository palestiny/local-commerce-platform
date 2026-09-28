# M1 Delivery Vertical Slice Design Gate

Status: **DESIGN IN PROGRESS — PERSISTENCE FOUNDATION GREEN**

## Objective

Define and verify the first Delivery vertical slice without violating ADR-007:

- Order owns commercial state through `READY_FOR_PICKUP`.
- Delivery owns fulfillment state from `UNASSIGNED` through `DELIVERED`, with `FAILED` as its exception state.
- No duplicate mutable fulfillment state is added to Order.

The gate must be passed before Delivery production implementation.

## Scope

M1 first slice:

1. Merchant marks Order `PREPARING -> READY_FOR_PICKUP`.
2. A Delivery is created for the ready Order.
3. An authorized driver is assigned.
4. Driver confirms pickup.
5. Driver moves Delivery to `OUT_FOR_DELIVERY`.
6. Driver completes Delivery as `DELIVERED`.
7. Delivery can enter `FAILED` through explicitly defined failure semantics.
8. Customer-facing read composes Order + Delivery state.

Explicitly deferred:

- driver location tracking
- route optimization
- batching
- dynamic assignment
- proof-of-delivery media
- advanced dispatch scoring
- live maps
- push notification infrastructure
- payment completion changes

## State Authority

### Order

`CREATED -> PENDING_STORE_CONFIRMATION -> ACCEPTED -> PREPARING -> READY_FOR_PICKUP`

Exceptions:

- `REJECTED`
- `CANCELLED`

Order must never own or transition:

- `ASSIGNED`
- `PICKED_UP`
- `OUT_FOR_DELIVERY`
- `DELIVERED`

### Delivery

`UNASSIGNED -> ASSIGNED -> PICKED_UP -> OUT_FOR_DELIVERY -> DELIVERED`

Exception:

- `FAILED`

Delivery is the sole mutable authority for fulfillment state.

## Delivery Aggregate Minimum Model

Proposed fields:

- DeliveryId
- OrderId
- StoreId
- DriverId (nullable while UNASSIGNED)
- Status
- CreatedAt
- UpdatedAt
- AssignedAt (nullable)
- PickedUpAt (nullable)
- OutForDeliveryAt (nullable)
- DeliveredAt (nullable)
- FailedAt (nullable)
- FailureReason (nullable)

Required invariant:

- one active Delivery per Order

The Delivery aggregate must not duplicate the Order commercial status.

## Authorization Rules

- Merchant may transition its Store's Order to READY_FOR_PICKUP.
- Driver may perform Delivery commands only when assigned to that Delivery.
- A driver cannot assign another driver.
- Assignment is an operational command, not a direct arbitrary status update.
- Admin may perform explicitly authorized operational interventions.
- Customer has read access only to its own Order/Delivery representation.
- Every sensitive command is idempotent.

## Cross-Aggregate Boundary

### Proposed default

Use one application-level business operation for:

`PREPARING -> READY_FOR_PICKUP` + Delivery creation.

The application coordinates the two aggregates while each aggregate validates its own transition.

Recommended M1 persistence behavior:

- execute both changes inside one PostgreSQL transaction when they are part of the same command;
- commit both or neither;
- do not expose an intermediate customer-visible state in which the Order is READY_FOR_PICKUP but no Delivery exists because the creation operation partially committed.

This preserves separate aggregate ownership while giving the application a reliable business-operation boundary.

### Alternative

Use an application event/outbox flow:

- Order becomes READY_FOR_PICKUP.
- Delivery creation is triggered asynchronously.
- Temporary state requires a defined `DeliveryPending` operational/read condition.

Trade-off: better decoupling and future scalability, but more failure/retry machinery and a temporary cross-aggregate inconsistency.

For M1, the transactional application operation is the simpler implementation baseline. Outbox becomes justified when durable external/asynchronous integration is actually required.

## Delivery Creation Invariants

Delivery creation is valid only when:

- Order exists.
- Order belongs to the expected Store.
- Order is `READY_FOR_PICKUP`.
- Order is not `CANCELLED` or `REJECTED`.
- No active Delivery already exists for the Order.

The database should enforce one active Delivery per Order.

## Assignment

Assignment command:

`AssignDriver(DeliveryId, DriverId, IdempotencyKey)`

Preconditions:

- Delivery is `UNASSIGNED`.
- Driver is active.
- Driver is eligible for the operational scope of the Store/order.
- Delivery has no existing DriverId.

Result:

`UNASSIGNED -> ASSIGNED`

Concurrent assignment attempts must converge deterministically: only one driver becomes authoritative.

The exact driver eligibility/zone model is an M2 operational concern; M1 may use a minimal active-driver relationship without implementing dispatch optimization.

## Pickup

`ASSIGNED -> PICKED_UP`

Preconditions:

- Delivery has an assigned driver.
- Command actor is that assigned driver or an explicitly authorized operational actor.
- Delivery is currently ASSIGNED.

No Order fulfillment status is mutated.

## Out for Delivery

`PICKED_UP -> OUT_FOR_DELIVERY`

Preconditions:

- assigned driver relationship remains valid.
- current Delivery status is PICKED_UP.

No Order fulfillment status is mutated.

## Completion

`OUT_FOR_DELIVERY -> DELIVERED`

Preconditions:

- assigned driver relationship remains valid.
- current Delivery status is OUT_FOR_DELIVERY.

Completion must not silently mutate Payment state.

## Failure Semantics

Delivery may transition to `FAILED` only through an explicit failure command.

The first implementation should require:

- failure reason/code
- actor
- timestamp
- previous status
- idempotency key for retried sensitive commands

A FAILED Delivery is terminal for the current Delivery attempt in M1 unless a recovery/re-dispatch policy is explicitly introduced.

The initial M1 design should therefore record the failure and expose it operationally rather than inventing automatic retry behavior.

## Cancellation

Cancellation remains an Order commercial command.

Rules to finalize in M1:

- cancellation before Delivery creation affects Order only;
- cancellation after Delivery creation must define whether active Delivery is cancelled/terminated through a coordinated application operation;
- Delivery must not independently rewrite Order to CANCELLED.

The cancellation policy must be finalized before implementing cross-aggregate cancellation.

## Customer-Facing Read Model

Do not add a mutable DeliveryStatus field to Order.

Proposed composed response:

- Order commercial status
- Delivery status, nullable before Delivery exists
- Driver-facing delivery progress when applicable
- timestamps relevant to the current lifecycle
- derived customer-facing phase if needed

The composition can initially be a synchronous application query over Order + Delivery.

A projection/read model is deferred until measured query or scaling requirements justify it.

## Idempotency

Idempotency is required for:

- Delivery creation
- driver assignment
- pickup
- out-for-delivery transition
- delivery completion
- explicit delivery failure
- coordinated cancellation where it spans aggregates

Same key + same fingerprint must return the original result.

Same key + different fingerprint must be rejected.

Concurrent identical commands must not produce duplicate state transitions or duplicate Deliveries.

## Audit History

Every Delivery transition must create immutable history containing:

- DeliveryId
- actor type/id
- timestamp
- previous status
- new status
- command/correlation identifier
- failure reason when applicable

The history is append-only and is not the source of current state; Delivery remains the current-state authority.

## TDD RED Gate

Before implementation, add failing tests for:

### Delivery domain

- valid lifecycle transitions
- invalid transitions
- assignment requires UNASSIGNED
- pickup requires assigned driver
- out-for-delivery requires PICKED_UP
- delivered requires OUT_FOR_DELIVERY
- failure rules
- no duplicate driver assignment effect

### Application

- READY_FOR_PICKUP + Delivery creation succeeds atomically
- failure during Delivery creation rolls back the coordinated operation
- duplicate Delivery creation is rejected/converges
- concurrent driver assignment has one authoritative result
- unauthorized driver commands are rejected
- same-key replay is idempotent
- fingerprint conflict is rejected
- customer read composes Order + Delivery without duplicating state

### Persistence

- unique active Delivery per Order
- Delivery state persistence
- assignment concurrency
- rollback of coordinated Order + Delivery operation
- Delivery history persistence

## Design Decisions Still Open

1. Exact Delivery failure recovery policy.
2. Cancellation after Delivery creation.
3. Minimal Driver/eligibility model for M1.
4. Whether READY_FOR_PICKUP and Delivery creation must always be one command/transaction or whether an explicit asynchronous boundary is needed.
5. Exact customer-facing composed DTO.
6. Delivery history schema and retention policy.

## Gate Exit Criteria

The gate becomes PASS when:

1. Delivery aggregate ownership is explicit.
2. Delivery lifecycle transition table is finalized.
3. READY_FOR_PICKUP / Delivery creation boundary is decided.
4. Assignment, pickup, transit, completion, and failure semantics are finalized.
5. Cancellation semantics are finalized.
6. Authorization rules are finalized.
7. Idempotency requirements are finalized.
8. Customer-facing composed read model is finalized.
9. RED tests are written for the approved behavior.
10. No rule requires Order to duplicate mutable Delivery state.

## Current Evidence

- ADR-007 is ACCEPTED: strict Order/Delivery state separation.
- Order lifecycle document already reflects Delivery as a separate lifecycle.
- PostgreSQL persistence foundation is GREEN VERIFIED.
- M1 Delivery implementation has not started yet.

## Next Step

Finalize the open M1 decisions, then implement TDD RED tests before Delivery production code.
