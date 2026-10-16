# Persistence & Reliability Design Gate

Status: **PASS — GREEN VERIFIED**

## Objective

Prove that the first commercial vertical slice remains correct under real persistence, transactions, retries, and concurrency.

The application contract is already GREEN at the unit level. This gate moves the proof boundary from in-memory fakes to PostgreSQL.

## Proposed Persistence Stack

### Option A — EF Core + PostgreSQL — RECOMMENDED

- Entity Framework Core as the persistence implementation.
- Npgsql PostgreSQL provider.
- Application ports remain unchanged.
- Domain objects are mapped through Infrastructure configuration.
- Database transactions implement `ICreateOrderUnitOfWork`.

**Why this fits the current architecture**

- Natural fit for ASP.NET Core/.NET 8.
- Keeps persistence details outside Domain and Application.
- Supports explicit transactions and optimistic concurrency.
- Migrations provide a controlled schema evolution path.
- Integration tests can exercise the same relational behavior used in production.

### Option B — Dapper + PostgreSQL

- SQL-first persistence.
- Explicit SQL and mapping code.
- Greater control over generated SQL and query shape.
- More manual mapping, transaction plumbing, and change tracking.

### Trade-off

For M0, the main requirement is reliable transactional behavior rather than maximum SQL control. EF Core reduces infrastructure code while still allowing explicit transactions and database constraints. Dapper remains a valid future choice if measured query requirements justify it.

**Decision accepted:** Option A — EF Core + PostgreSQL for M0.

## Transaction Boundary

Create Order must execute as one database transaction containing:

1. Idempotency reservation.
2. Order insert.
3. OrderItem inserts.
4. Cart consumption.
5. Idempotency completion.

If any step fails, the transaction rolls back.

A retry after rollback must be able to execute safely again.

## Idempotency Persistence

Proposed table concept:

`IdempotencyRecords`

Required fields:

- CustomerId / ActorId
- Operation
- IdempotencyKey
- RequestFingerprint
- Status
- OrderId
- OrderNumber
- CreatedAt
- CompletedAt

Required database invariant:

- unique `(CustomerId, Operation, IdempotencyKey)`

The application-level `ReserveAsync` contract maps to an atomic database operation:

- no existing row → create reservation
- existing row → return existing record
- unique-key race → resolve to the persisted existing record, not create a second Order

A reservation must not be treated as a completed result until the Order and Cart changes commit successfully.

## Order Persistence

Proposed relational structure:

### Orders

- Id — primary key
- StoreId — required foreign key
- OrderNumber — required unique business identifier
- Status — required
- CreatedAt / UpdatedAt

### OrderItems

- Id — primary key
- OrderId — required foreign key
- ProductId — required
- StoreId — required
- ProductName — required snapshot
- VariantName — nullable snapshot
- UnitPrice — fixed precision decimal
- Quantity — positive integer
- LineDiscount — fixed precision decimal
- LineTotal — fixed precision decimal

OrderItem rows are historical snapshots. Future Catalog changes must not mutate them.

## Money

M0 will use fixed-precision decimal for persisted monetary values, with explicit currency introduced when the Payment/Money design is finalized.

No floating-point money representation is permitted.

The exact precision/scale must be fixed before migrations are created.

## Cart Consumption

The database must prevent two concurrent Create Order operations from consuming the same active Cart.

The proposed M0 model is:

- Cart has an explicit lifecycle/consumed marker.
- Successful Create Order changes the Cart to consumed within the same transaction.
- Consumption is conditional on the Cart still being active.
- A failed conditional update produces a deterministic concurrency rejection.
- The same idempotency key remains replayable through the idempotency record.

The Cart persistence schema and checkout concurrency behavior are implemented and verified in the real PostgreSQL integration slice.

## Concurrency Strategy

Default strategy:

- database transactions for atomic multi-row changes
- optimistic concurrency for mutable aggregates where required
- unique constraints for identity/invariants
- conditional updates for state-sensitive mutations

Pessimistic locks are not the default.

For Create Order, the integration tests must prove:

1. Two concurrent attempts against the same Cart cannot create two Orders.
2. Two concurrent attempts with the same idempotency key converge to one result.
3. A failed transaction leaves no partial Order/OrderItem/Cart/idempotency state.
4. A successful transaction consumes the Cart exactly once.

## Product Availability Race

The application currently reads ProductSnapshot data before entering the transaction.

For M0, the Product persistence design must explicitly decide whether checkout is:

- snapshot-based without inventory reservation, or
- protected by a product version/availability concurrency check.

Because inventory reservation is an explicit M0 non-goal, the initial recommendation is **no inventory reservation**. Product orderability is validated from authoritative server state at checkout; stock reservation is deferred to the Inventory design.

This means M0 must not claim strong inventory guarantees that it does not implement.

## Order Number

OrderNumber is a customer-facing business identifier and must be database-unique.

The generator may create a candidate value, but the database remains the final uniqueness authority.

Collision handling must be deterministic and must not create a duplicate commercial Order.

## Database Constraints

At minimum:

- primary keys
- required foreign keys
- unique OrderNumber
- unique Customer + Operation + IdempotencyKey
- positive Quantity constraint
- required monetary fields
- valid Order status representation

Indexes should be introduced only from verified query patterns.

## Integration Test Gate

Unit tests are insufficient for the persistence claims above.

M0 requires PostgreSQL-backed integration tests covering:

- successful Create Order
- transaction rollback after a forced persistence failure
- Cart consumption
- duplicate idempotency replay
- idempotency fingerprint conflict
- concurrent same-key requests
- concurrent different-key requests against one Cart
- Order/OrderItem snapshot persistence
- OrderNumber uniqueness
- persistence constraints

The tests must execute against a real PostgreSQL instance, not an in-memory substitute.

## Migration and Recovery

The M0 persistence path now has executable recovery evidence:

- the initial PostgreSQL schema is version-controlled as `001_initial.sql`
- database initialization is repeatable through the embedded initializer
- CI executes a PostgreSQL backup with `pg_dump`
- CI drops and recreates the database, restores with `pg_restore`, and verifies a sentinel row
- the restore smoke test completed successfully in GitHub Actions run `36474156793` (run 82), job `109103551191`

## Explicit Non-Goals

- Redis
- Kafka/RabbitMQ
- distributed transactions
- inventory reservation
- payment processing
- delivery persistence
- API controllers
- microservices

## Gate Exit Criteria

This gate is **PASS** because all exit criteria are now evidenced:

1. Persistence technology decision accepted: ADR-008, EF Core + PostgreSQL.
2. Entity mappings defined and implemented.
3. Transaction boundary implemented for reservation, Order/OrderItems, Cart consumption, and completion.
4. Database constraints implemented, including uniqueness, foreign keys, and positive quantities.
5. Real PostgreSQL integration suite passes: 5/5 tests in CI.
6. Concurrency behavior verified for same-key idempotency and competing keys against one Cart.
7. Rollback behavior verified after an Order write.
8. Migration/initialization and backup/restore path verified by CI smoke test.

## Current Status

Application layer: **GREEN VERIFIED**.

Persistence layer: **GREEN VERIFIED**.

The next design/implementation boundary is the Delivery vertical slice. HTTP/API work remains intentionally sequenced after the core domain/application/persistence gates.
