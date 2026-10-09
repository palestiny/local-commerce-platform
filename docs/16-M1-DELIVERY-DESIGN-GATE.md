# M1 Delivery Vertical Slice Design Gate

Status: **M1 DELIVERY CONCURRENCY GREEN VERIFIED — NEXT: API CONTRACT/SECURITY GATE**

## Objective

Define and verify the first Delivery vertical slice without violating ADR-007:

- Order owns commercial state through `READY_FOR_PICKUP`.
- Delivery owns fulfillment state from `UNASSIGNED` through `DELIVERED`, with `FAILED` and `CANCELLED` as explicit exception states.
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
8. A failed attempt can be replaced explicitly when the Order remains eligible.
9. Customer-facing read composes Order + Delivery state.
10. Cancellation coordinates Order + active Delivery before pickup.

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

Explicit exception states:

- `FAILED`
- `CANCELLED`

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

## Replacement Delivery

A failed Delivery remains terminal.

`CreateReplacementDelivery` is an explicit operational command that may create a new `UNASSIGNED` Delivery only when:

- the Order remains eligible;
- no active Delivery exists;
- the request is authorized and idempotent.

The failed attempt remains in history; the replacement is a new Delivery record.

## Cancellation

Cancellation remains an Order commercial command.

Accepted M1 policy:

- before Delivery exists: normal Order cancellation;
- after Delivery exists but before pickup: coordinated application cancellation atomically cancels the Order and terminates the active Delivery attempt;
- after pickup: ordinary customer/merchant cancellation is rejected; only explicitly authorized operational intervention may handle the case;
- Payment/refund behavior remains a separate Payment decision.

Delivery does not independently rewrite Order to CANCELLED.

The Delivery aggregate therefore has an explicit `CANCELLED` state for coordinated pre-pickup termination. Cancellation does not use `FAILED`, preserving the semantic distinction between operational failure and commercial cancellation.

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
- replacement Delivery
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
- cancellation-before-pickup rules
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
- pre-pickup cancellation coordinates Order + active Delivery
- post-pickup cancellation is rejected

### Persistence

- unique active Delivery per Order
- Delivery state persistence
- assignment concurrency
- rollback of coordinated Order + Delivery operation
- Delivery history persistence

## Accepted M1 Decision Baseline — OWNER APPROVED

The following baseline is **ACCEPTED** by the project owner.

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
| UNASSIGNED / ASSIGNED | CancelOrder | Authorized customer/merchant actor | Delivery = CANCELLED | Coordinated with Order cancellation before pickup |

`FAILED` remains terminal for that Delivery record. A replacement creates a new Delivery attempt rather than reopening the failed record.

### Proposed cancellation rule

Cancellation is still an Order command and never originates from Delivery.

- Before Delivery exists: normal Order cancellation rules apply.
- After Delivery exists but before pickup: application-level cancellation may atomically cancel the Order and terminate the active Delivery attempt.
- After pickup: ordinary customer/merchant cancellation is rejected; only an explicitly authorized operational intervention may handle the case.
- Payment/refund behavior remains a separate Payment decision and is not silently inferred from Delivery state.

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

The project owner approved the business-policy choices:

1. cancellation after Delivery creation;
2. replacement Delivery after FAILED;
3. minimal Driver eligibility for M1.

The technical transaction boundary, state authority, idempotency, and no-duplicate-state rules remain aligned with the accepted architecture and ADR-007.

## Design Decisions Still Open

The original M1 business-policy questions are now closed. Remaining work is implementation/integration detail:

1. Customer-facing composed DTO exact contract.
2. Delivery history schema and retention policy.
3. Delivery persistence schema/migration.
4. PostgreSQL concurrency behavior for Delivery commands.

## Gate Exit Criteria

The gate becomes PASS when:

1. Delivery aggregate ownership is explicit.
2. Delivery lifecycle transition table is finalized.
3. READY_FOR_PICKUP / Delivery creation boundary is decided.
4. Assignment, pickup, transit, completion, failure, replacement, and cancellation semantics are finalized.
5. Authorization rules are finalized.
6. Idempotency requirements are finalized.
7. Customer-facing composed read model is finalized.
8. RED tests are written for the approved behavior.
9. No rule requires Order to duplicate mutable Delivery state.

## Current Evidence

- ADR-007 is ACCEPTED: strict Order/Delivery state separation.
- Order lifecycle document reflects Delivery as a separate lifecycle.
- PostgreSQL persistence foundation is GREEN VERIFIED.
- M1 Delivery domain/application increments through cancellation are implemented and verified by CI.
- API/HTTP remains intentionally deferred.

## Next Step

Implement and verify the customer-facing composed read contract, then address Delivery persistence/history and real PostgreSQL concurrency before API/HTTP implementation.

## TDD RED execution status

**Status: IN PROGRESS — Domain RED suite committed**

Initial RED specification was committed at:

- `tests/LocalCommerce.Domain.Tests/Delivery/DeliveryTests.cs`

The suite defines the first executable Delivery domain contract.

## Updated Implementation Status — 2026-09-28

**Design approval: CLOSED.**

The approved M1 business-policy baseline remains authoritative.

### Domain implementation

**Implemented and GREEN VERIFIED.**

`src/LocalCommerce.Domain/Delivery/Delivery.cs` implements:

- UNASSIGNED -> ASSIGNED
- ASSIGNED -> PICKED_UP
- PICKED_UP -> OUT_FOR_DELIVERY
- OUT_FOR_DELIVERY -> DELIVERED
- explicit FAILED terminal state
- explicit CANCELLED pre-pickup state
- assigned-driver validation for pickup
- required Order/Store identity
- terminal-state protection

No mutable Delivery fulfillment state was added to Order.

### First M1 application slice

**GREEN VERIFIED.**

`PREPARING -> READY_FOR_PICKUP` + Delivery creation is implemented and verified as one application operation.

GitHub Actions run `36483288423` (#128), commit `7cb7f7cc099d3c84014449fe6d08c83ab50565de`, job `109133935990`: `Test` and `Migration and recovery smoke test` both passed.

## Updated Driver Assignment TDD Status — 2026-09-29

**Status: GREEN VERIFIED.**

`AssignDriverCommand(DeliveryId, DriverId, ActorId, IdempotencyKey)` is implemented and verified for active-driver validation, authorization, lifecycle preconditions, replay, fingerprint conflict, and unit-of-work boundary.

GitHub Actions run `36596803406` (#152), commit `beddffa6a373ee0f59d8a98bd604354ccc97759b`, job `109503704013`: `Test` and `Migration and recovery smoke test` both passed.

## Updated Pickup TDD Status — 2026-09-30

**Status: GREEN VERIFIED.**

`ConfirmPickupCommand(DeliveryId, ActorId, IdempotencyKey)` is implemented and verified.

GitHub Actions run `36599194263` (#164), commit `8e3d8788724f30b0c0d4d1f505df03dbf9aa7bfd`, job `109511861790`: `Test` and `Migration and recovery smoke test` both passed.

## Updated Start Delivery TDD Status — 2026-09-30

**Status: GREEN VERIFIED.**

`StartDeliveryCommand(DeliveryId, ActorId, IdempotencyKey)` is implemented and verified.

GitHub Actions run `36729575121` (#171), commit `2dcc22349baee9918e39305453f7b656202f55ee`, job `109935288931`: `Test` and `Migration and recovery smoke test` both passed.

## Updated Complete Delivery TDD Status — 2026-09-30

**Status: GREEN VERIFIED.**

`CompleteDeliveryCommand(DeliveryId, ActorId, IdempotencyKey)` is implemented and verified.

GitHub Actions run `36737706814` (#178), job `109963659983`: `Test` and `Migration and recovery smoke test` both passed.

## Updated Failure Verification — 2026-09-30

**Status: GREEN VERIFIED.**

FailDelivery is implemented and verified for active-delivery failure, authorization, terminal-state protection, required failure code/reason, idempotent replay, fingerprint conflict, and transaction boundary.

GitHub Actions run `36738000967` (#185), job `109964662755`: `Test` and `Migration and recovery smoke test` both passed.

## Updated Replacement Delivery Verification — 2026-09-30

**Status: GREEN VERIFIED.**

`CreateReplacementDelivery` is implemented and verified for replacement after a failed attempt, active-delivery prevention, Order eligibility, idempotent replay, and request fingerprint conflict. The failed Delivery record remains terminal; replacement creates a new `UNASSIGNED` Delivery attempt.

GitHub Actions run `36739367541` (#191), job `109969385152`: `Test` and `Migration and recovery smoke test` both passed.

## Updated Cancellation Verification — 2026-09-30

**Status: GREEN VERIFIED.**

Cancellation coordination is implemented and verified for:

- Order cancellation with an active pre-pickup Delivery;
- explicit Delivery `CANCELLED` state rather than reusing `FAILED`;
- `READY_FOR_PICKUP` Order cancellation;
- rejection after pickup;
- Order cancellation when no Delivery exists;
- same-key idempotent replay;
- fingerprint conflict;
- unit-of-work failure boundary.

Latest verified commit: `ec4b1d2baa020c0fc85e224259976505758aa5a6`.

GitHub Actions push run `36743454949` (#205), job `109983538827`, and pull-request run `36743462378` (#206), job `109983563672`, both completed successfully. `Test` and `Migration and recovery smoke test` passed in both runs.

### Verification boundary

At this historical milestone, cancellation was verified with application/domain test doubles only. Later PostgreSQL persistence and concurrency evidence is documented in the sections below.

Customer Composed Read is GREEN VERIFIED. The following implementation increments subsequently completed Delivery persistence/history and the defined PostgreSQL concurrency scenarios.


## Updated Customer Composed Read Verification — 2026-10-01

**Status: GREEN VERIFIED.**

The customer-facing composed read is implemented as a non-mutating application query composing Order commercial state with an optional active Delivery summary. Customer ownership is enforced through the application authorization boundary, and the response does not duplicate mutable Delivery state onto Order.

Verified scenarios include: own Order with Delivery summary, own Order before Delivery exists, cross-customer rejection, missing Order rejection, and read non-mutation.

GitHub Actions run `36785578476` (#220), job `110126060648`, commit `e630684b46b8b84dd7674a948db6444d3785519a`: `Test` and `Migration and recovery smoke test` both passed.

### Verification boundary

At this historical milestone, this proved the application/domain contract using test-double repositories. Subsequent PostgreSQL persistence and concurrency evidence is documented below.

Historical next boundary: Delivery persistence + append-only DeliveryStatusHistory, followed by real PostgreSQL concurrency verification. Both have since been implemented for the documented M1 scope.


## Updated Delivery Persistence + History Verification — 2026-10-01

**Status: GREEN VERIFIED.**

Delivery persistence is now implemented and verified against real PostgreSQL. The persistence boundary covers current Delivery state, active-Delivery lookup, append-only DeliveryStatusHistory, and the database-enforced rule that only one active Delivery can exist for an Order.

The schema includes a partial unique index on active Delivery states. DeliveryStatusHistory is protected by a database trigger that rejects UPDATE/DELETE, preserving append-only semantics at the persistence boundary.

GitHub Actions run `36822196856` (#240), job `110239982378`, commit `e3cc70567db20e0e051d01128eb569d9c282fcb4`: `Test` and `Migration and recovery smoke test` both passed.

### Verification boundary

At this historical milestone, this proved Delivery persistence/schema and basic history persistence against PostgreSQL. Subsequent concurrency verification is recorded below. End-to-end history creation for every application command remains a separate coverage consideration and must not be inferred from the concurrency tests.

The next boundary after the subsequent concurrency verification is API contract/security design and HTTP implementation.


## Updated Delivery Concurrency Strategy — 2026-10-01

**Status: STRATEGY ACCEPTED — DEFINED M1 COMMAND CONCURRENCY GREEN VERIFIED.**

ADR-009 accepts PostgreSQL row-level locking for M1 Delivery mutation commands.

- Mutable Delivery loads inside an active mutation transaction use `SELECT FOR UPDATE`.
- The transaction must begin before the mutable Delivery load.
- Read-only customer queries remain non-locking.
- The existing active-Delivery partial unique index remains the database backstop.
- The transaction-boundary refactor and PostgreSQL idempotency persistence verification have since completed; see the final verification section below.

A real PostgreSQL persistence test now verifies that a second transactional Delivery load waits for the first writer and observes the committed state.

The refactor and the defined PostgreSQL contention tests are complete. Continue with API contract/security design before HTTP implementation.

### PostgreSQL concurrency verification

**Current status: GREEN VERIFIED for the defined M1 concurrency scenarios.**

Verified after commit `e2bc8cf85878510f13dd5697a909b4876af98a10`:
- CI run #264 (`36825487024`) completed successfully.
- Delivery persistence test suite verifies a second PostgreSQL transaction waits while the first transaction holds the Delivery row lock, then observes the committed state.
- Delivery mutation handlers now load mutable Delivery state inside the active Unit of Work for AssignDriver, ConfirmPickup, StartDelivery, CompleteDelivery, and FailDelivery.

Verified on PostgreSQL in GitHub Actions CI run `37861612028`, commit `fdba8b7524b8550ef707fc80780327c9a8dce189`:
- Competing driver assignments produce one success and one domain rejection.
- Competing replacement-delivery creations converge to one active Delivery.
- Concurrent cancellations serialize on the Order row.
- Cancellation-versus-replacement leaves no active Delivery for a cancelled Order.
- Replacement eligibility uses persisted Order state while the Order lock is held.
- Concurrent ConfirmPickup, StartDelivery, CompleteDelivery, and FailDelivery each have one winner; the loser raises the operation-specific rejection exception.
- Idempotency persistence and migration/recovery smoke tests pass.

Scope limitation: this closes the listed M1 scenarios, not every possible cross-command interleaving or production-load behavior. End-to-end DeliveryStatusHistory creation coverage for every command should be reviewed separately.
