# M0 Create Order — Application Design Gate

Status: **READY FOR TDD RED**

## Scope

This gate defines the application boundary for the first executable vertical slice:

Customer → Store → Product → Cart → Create Order

It does not implement HTTP/API concerns and does not create Delivery state.

## Accepted application responsibilities

The Create Order application service is responsible for orchestration, not business state ownership.

1. Load the Customer-owned Cart.
2. Authorize that the Cart belongs to the requesting Customer.
3. Require the Cart to be active and non-empty.
4. Resolve the Cart Store and require the Store to be active.
5. Resolve every Product and require each product to be currently orderable.
6. Recalculate purchase pricing from server-side Product data.
7. Create immutable OrderItem purchase snapshots.
8. Create the Order through the Domain aggregate.
9. Persist Order + OrderItems + Cart checkout mutation atomically.
10. Return a customer-facing Order number.
11. Apply idempotency before producing a second commercial Order.

## Cart checkout decision

A successful Create Order **consumes the active Cart** as part of the same transaction.

- Success: Cart becomes unavailable for another checkout.
- Failure before commit: Cart remains active.
- Retry with the same idempotency key: returns the original result.
- A different idempotency key against an already-consumed Cart is rejected.

This prevents accidental duplicate commercial Orders from a reusable active Cart.

## Application ports

The initial handler depends on abstractions for:

- Cart read/update
- Store read
- Product read
- Order write
- Idempotency record read/write
- Transaction/unit-of-work
- Order number generation

Infrastructure owns their concrete implementations.

## Pricing rule

Client-provided totals and prices are never authoritative.

The application resolves current server-side product pricing and calculates line totals/order totals before creating the Domain Order.

Discount policy is intentionally not introduced in this slice. The existing snapshot fields remain available for the later pricing/promotion design.

## Idempotency

Idempotency is scoped to the Customer + operation.

The stored record contains:

- Idempotency key
- Deterministic request fingerprint
- Original Order identifier/number
- Final outcome

Same key + same fingerprint returns the original result.

Same key + different fingerprint is rejected.

The persistence layer must enforce uniqueness so concurrent requests cannot create two commercial Orders.

## Transaction boundary

The successful path is one atomic consistency boundary containing:

- Order persistence
- OrderItem persistence
- Cart consumption
- Idempotency completion record

Exact database mechanism remains an Infrastructure concern and must satisfy the application contract.

## Authorization

Authorization is enforced from server-side ownership data:

Customer → Cart → Store.

A caller cannot select another customer's Cart merely by supplying its identifier.

## Explicit non-goals

- HTTP controllers/endpoints
- Authentication provider implementation
- Payment
- Delivery creation/assignment
- Notifications
- Promotions/discount engine
- Inventory reservation
- distributed messaging

## Gate exit criteria

- Application command and result contract is explicit.
- Required ports are explicit.
- Cart consumption semantics are explicit.
- Idempotency semantics are explicit.
- Transaction boundary is explicit at the application level.
- RED tests can be written without inventing unresolved business rules.

## Next step

Create the executable Create Order RED suite. Production behavior remains intentionally unimplemented until the tests establish the contract.
