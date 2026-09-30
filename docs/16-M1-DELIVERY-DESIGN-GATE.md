# M1 Delivery Vertical Slice Design Gate

Status: **M1 PICKUP GREEN VERIFIED — START DELIVERY NEXT**

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

## Accepted M1 Decision Baseline — OWNER APPROVED

The following baseline is recommended for the first vertical slice. These decisions are **ACCEPTED** by the project owner.

### Decision matrix

| Decision | Proposed M1 baseline | Why | Deferred complexity |
|---|---|---|---|
| READY + Delivery creation | One application command, one PostgreSQL transaction | Prevents READY-without-Delivery partial state | Outbox/async orchestration |
| Delivery failure | FAILED is terminal for the current Delivery attempt | Keeps failure semantics explicit and avoids invented retries | Automated retry/re-dispatch |
| Failed delivery recovery | Explicit operational `CreateReplacementDelivery` only when no active Delivery exists and Order remains eligible | Preserves failed attempt history while allowing controlled recovery | Automatic re-dispatch |
| Cancellation after Delivery exists | Customer/merchant cancellation allowed only before pickup; after pickup requires explicit admin operational handling | Avoids silently reversing a fulfillment operation already in motion | Refund/dispute automation |
| Driver model | Minimal Driver identity + Active/Inactive state; assignment performed by authorized operational actor | Enough for M1 without prematurely building dispatch | Zones, capacity, scoring, auto-dispatch |
| Driver eligibility | M1 validates active Driver only; Store/zone eligibility is an explicit future constraint | Avoids pretending an unmodeled zone system exists | Zone/capacity/vehicle rules |
| Customer read | Synchronous composed application DTO: Order commercial state + nullable Delivery summary + lifecycle timestamps | Simple, authoritative, no duplicated mutable state | Projection/read model |
| Delivery history | Append-only `DeliveryStatusHistory`; actor, timestamp, previous/new state, command/correlation id, failure code/reason | Auditable and consistent with Order history | Retention/archival subsystem |

### Proposed Delivery transition table

| Current | Command | Actor | Next | Notes |
|---|---|---|---|---|
| UNASSIGNED | AssignDriver | Authorized operator | ASSIGNED | Driver must be active |
| ASSIGNED | ConfirmPickup | Assigned driver / authorized operator | PICKED_UP | No Order fulfillment mutation |
| PICKED_UP | StartDelivery | Assigned driver / authorized operator | OUT_FOR_DELIVERY | No Order fulfillment mutation |
| OUT_FOR_DELIVERY | CompleteDelivery | Assigned driver / authorized operator | DELIVERED | Payment remains independent |
| UNASSIGNED / ASSIGNED / PICKED_UP / OUT_FOR_DELIVERY | FailDelivery | Authorized operational actor | FAILED | Explicit reason required |
| FAILED | CreateReplacementDelivery | Authorized operational actor | new Delivery = UNASSIGNED | Only if Order is still eligible and no active Delivery exists |

`FAILED` remains terminal for that Delivery record. A replacement creates a new Delivery attempt rather than reopening the failed record.

### Proposed cancellation rule

Cancellation is still an Order command and never originates from Delivery.

- Before Delivery exists: normal Order cancellation rules apply.
- After Delivery exists but before pickup: application-level cancellation may atomically cancel the Order and terminate the active Delivery attempt.
- After pickup: ordinary customer/merchant cancellation is rejected; only an explicitly authorized operational intervention may handle the case.
- Payment/refund behavior remains a separate Payment decision and is not silently inferred from Delivery state.

This keeps commercial cancellation authoritative in Order while preventing an active fulfillment attempt from being left ambiguous.

### Proposed customer-facing DTO

The initial read model should compose, rather than duplicate:

- `OrderId`
- `OrderNumber`
- `OrderStatus`
- `Order timestamps`
- `Delivery` nullable
  - `DeliveryId`
  - `Status`
  - `AssignedAt`
  - `PickedUpAt`
  - `OutForDeliveryAt`
  - `DeliveredAt`
  - `FailedAt`
  - `FailureReason` when appropriate

No mutable `DeliveryStatus` field is added to Order.

### Approval boundary — CLOSED

The project owner approved the proposed business-policy choices:

1. cancellation after Delivery creation;
2. replacement Delivery after FAILED;
3. minimal Driver eligibility for M1.

The technical transaction boundary, state authority, idempotency, and no-duplicate-state rules remain aligned with the accepted architecture and ADR-007.

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


## TDD RED execution status

**Status: IN PROGRESS — Domain RED suite committed**

Initial RED specification has been committed at:
- `tests/LocalCommerce.Domain.Tests/Delivery/DeliveryTests.cs`

The suite defines the first executable Delivery domain contract for:
- initial UNASSIGNED state
- driver assignment and duplicate-assignment rejection
- assigned-driver-only pickup
- pickup → out-for-delivery → delivered lifecycle
- invalid lifecycle transitions
- terminal FAILED semantics
- terminal DELIVERED semantics
- required Order/Store identity invariants

Production Delivery implementation has **not** been added yet. The next implementation step is GREEN: introduce the minimum Delivery aggregate required to satisfy these tests without expanding M1 scope.


## Updated Implementation Status — 2026-09-28

**Design approval: CLOSED.**

The previously approved M1 business-policy baseline remains authoritative.

### Domain implementation

**Implemented and GREEN VERIFIED.**

src/LocalCommerce.Domain/Delivery/Delivery.cs now implements the minimum Delivery aggregate required by the approved RED contract:

- UNASSIGNED -> ASSIGNED
- ASSIGNED -> PICKED_UP
- PICKED_UP -> OUT_FOR_DELIVERY
- OUT_FOR_DELIVERY -> DELIVERED
- explicit FAILED terminal state
- assigned-driver validation for pickup
- required Order/Store identity
- terminal-state protection

No mutable Delivery fulfillment state was added to Order.

### Application TDD RED

**Status: GREEN VERIFIED — first application slice.**

The first M1 application slice is now defined in:

- src/LocalCommerce.Application/Delivery/ReadyOrderForDeliveryApplication.cs
- tests/LocalCommerce.Application.Tests/Delivery/ReadyOrderForDeliveryHandlerTests.cs

The contract covers:

- PREPARING -> READY_FOR_PICKUP + Delivery creation as one application operation
- prevention of an existing active Delivery
- authorization boundary for the actor marking the Order ready
- same-key idempotent replay
- same-key fingerprint conflict
- unit-of-work failure boundary

The current implementation is intentionally the next GREEN target; the application behavior must be verified before expanding into driver commands.

### Verification state

GREEN VERIFIED by GitHub Actions run `36483288423` (run #128) for commit `7cb7f7cc099d3c84014449fe6d08c83ab50565de`.

The `build-and-test` job `109133935990` completed successfully. Both `Test` and `Migration and recovery smoke test` steps passed.

### Next TDD increment

GREEN the first application slice, then add the next RED contract for:

1. active Driver validation and assignment;
2. assigned-driver pickup authorization;
3. pickup -> out-for-delivery -> delivered application commands;
4. failure and replacement Delivery;
5. coordinated cancellation;
6. composed customer-facing Delivery read;
7. persistence constraints/history and PostgreSQL concurrency verification.

API implementation remains intentionally deferred.


## Updated Driver Assignment TDD Status — 2026-09-29

**Status: GREEN VERIFIED.**

The next application increment is intentionally limited to Driver Assignment.

### Approved M1 contract

Command:

`AssignDriverCommand(DeliveryId, DriverId, ActorId, IdempotencyKey)`

Required behavior:

- Delivery must exist and be `UNASSIGNED`.
- Driver must exist and be Active.
- Actor must be authorized to perform operational assignment.
- Delivery domain remains responsible for the `UNASSIGNED -> ASSIGNED` transition.
- Same idempotency key + same fingerprint returns the original result.
- Same idempotency key + different request fingerprint is rejected.
- A unit-of-work failure must not leave a partially assigned Delivery.
- No Order state is mutated by assignment.

### RED artifacts

- `tests/LocalCommerce.Domain.Tests/Delivery/DriverTests.cs`
- `tests/LocalCommerce.Application.Tests/Delivery/AssignDriverHandlerTests.cs`

The Driver/application implementation is now present and verified against this contract.

### Scope boundary

Deferred from this increment:

- zone/store eligibility
- dispatch optimization
- capacity scoring
- auto-dispatch
- driver location
- HTTP/API endpoints

GitHub Actions run `36596803406` (run #152), commit `beddffa6a373ee0f59d8a98bd604354ccc97759b`, job `109503704013` completed successfully. Both `Test` and `Migration and recovery smoke test` passed. The next TDD increment is Start Delivery authorization.


## Updated Pickup TDD Status — 2026-09-30

**Status: GREEN VERIFIED.**

`ConfirmPickupCommand(DeliveryId, ActorId, IdempotencyKey)` is implemented and verified for assigned-driver authorization, non-assigned actor rejection, ASSIGNED precondition, same-key replay, fingerprint conflict, unit-of-work failure boundary, and Delivery-only fulfillment mutation.

GitHub Actions run `36599194263` (run #164), commit `8e3d8788724f30b0c0d4d1f505df03dbf9aa7bfd`, job `109511861790`: `Test` and `Migration and recovery smoke test` both passed.

Next TDD increment: Start Delivery.


## Updated Start Delivery TDD Status — 2026-09-30

**Status: GREEN VERIFIED.**

`StartDeliveryCommand(DeliveryId, ActorId, IdempotencyKey)` is implemented and verified for:

- PICKED_UP precondition;
- assigned-driver authorization;
- rejection of non-assigned actors;
- same-key replay;
- fingerprint conflict;
- unit-of-work boundary;
- Delivery-only transition to OUT_FOR_DELIVERY.

GitHub Actions run `36729575121` (run #171), commit `2dcc22349baee9918e39305453f7b656202f55ee`, job `109935288931`: `Test` and `Migration and recovery smoke test` both passed.

Next TDD increment: Complete Delivery.
