# ADR-009: PostgreSQL Concurrency Strategy for Delivery Commands

Status: **ACCEPTED**

## Context

Delivery commands mutate the same aggregate through short, transactional state transitions:

- AssignDriver
- ConfirmPickup
- StartDelivery
- CompleteDelivery
- FailDelivery
- coordinated pre-pickup cancellation

Application-level test doubles verify lifecycle rules and idempotency contracts, but they do not prove that two real PostgreSQL transactions cannot both read the same mutable Delivery state and then overwrite each other.

The current persistence model has a database uniqueness constraint for one active Delivery per Order, but that constraint alone does not prevent lost updates on an existing Delivery row.

## Options

### Option A — Optimistic concurrency token

Add a version/concurrency column and reject stale updates.

**Pros**
- High read concurrency.
- Standard optimistic concurrency model.

**Trade-offs**
- Every command must define retry/conflict behavior.
- More application handling is required for state-machine commands.
- A conflict is detected at write time rather than serialized at the aggregate boundary.

### Option B — PostgreSQL row-level locking

For mutable Delivery commands, begin the PostgreSQL transaction before loading the Delivery and load the row with SELECT FOR UPDATE.

**Pros**
- Serializes short state-transition commands on the same Delivery.
- The second transaction re-reads the committed/current row after waiting for the first transaction.
- Fits the current command model: short transactions, explicit state transitions, deterministic rejection.

**Trade-offs**
- Holding a database lock for the command duration reduces concurrency on the same Delivery.
- Command handlers must ensure the mutable load happens inside the transaction.
- Locking must not be used for ordinary read-only queries.

### Option C — Application-only synchronization

Use in-process locks or synchronization primitives.

**Pros**
- Simple in one process.

**Trade-offs**
- Fails across multiple application instances.
- Does not protect direct database writers.
- Not acceptable as the persistence correctness boundary.

## Decision

Adopt **Option B — PostgreSQL row-level locking** for M1 Delivery mutation commands.

Rules:

1. The transaction starts before the mutable Delivery is loaded.
2. A Delivery loaded inside an active mutation transaction uses SELECT FOR UPDATE.
3. Domain validation and mutation happen while the row lock is held.
4. Save and idempotency completion occur in the same transaction.
5. Ordinary customer/read-model queries remain non-locking.
6. The existing partial unique index remains the database backstop for one active Delivery per Order.
7. Optimistic concurrency tokens remain deferred unless measured contention or scaling requirements justify them.

## Consequences

- Delivery commands must not rely on a Delivery snapshot loaded before the Unit of Work.
- Existing application handlers that currently load Delivery before entering the Unit of Work require refactoring before their real PostgreSQL concurrency can be claimed GREEN.
- Persistence tests must prove that a concurrent command observes the post-commit state rather than applying a stale transition.
- Coordinated cancellation, ready-for-delivery, and replacement-delivery creation must lock the Order before inspecting or locking its Delivery. This also serializes the "no active Delivery exists" check against order cancellation, where locking only existing Delivery rows is insufficient.
- Replacement-delivery creation uses the database's unique active-delivery index as a final integrity backstop, not as its primary serialization strategy.

## Verification

Verified on PostgreSQL in CI at commit `c9e399db481c178030964377ab0d1bd2863d66c1`:

- Competing driver assignments produce one successful transition and one domain rejection.
- Competing replacement-delivery creations converge to one active Delivery.
- Concurrent order cancellations serialize on the Order row.
- Idempotency reservation, completion, and rollback persistence tests pass.
- Migration and recovery smoke test passes.

Additional verification is in progress on the newer commit:

- Race order cancellation against replacement-delivery creation, asserting a cancelled Order never retains an active Delivery.
- Check replacement eligibility against the persisted Order state while the Order row lock is held; only `ReadyForPickup` is eligible.

The gate remains **OPEN** until CI passes on the newest commit and the remaining Delivery transitions (ConfirmPickup, StartDelivery, CompleteDelivery, and FailDelivery) have explicit PostgreSQL concurrency coverage or a documented reason why their existing row-lock path is sufficient.