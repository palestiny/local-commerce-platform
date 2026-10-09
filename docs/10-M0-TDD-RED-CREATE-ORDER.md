# M0 TDD RED — Create Order

Status: **HISTORICAL RED SPECIFICATION — REQUIREMENTS RETAINED AS ACCEPTANCE REFERENCE**

## Objective

Turn the committed commerce invariants into the first executable tests before production implementation.

The first vertical slice is:

Customer -> Store -> Product -> Cart -> Create Order -> Merchant Order Inbox

Delivery is intentionally outside the Create Order command. Delivery begins from the separate Delivery lifecycle after the Order reaches READY_FOR_PICKUP.

## Test Layers

### Domain tests

1. Empty Order cannot exist.
2. Order must contain at least one item.
3. Order items belong to exactly one Store.
4. Purchased unit price is immutable after Order creation.
5. Valid Order commercial transitions are accepted.
6. Invalid Order transitions are rejected.
7. Order cannot transition directly into Delivery states.
8. Cancellation follows only explicitly allowed states.

### Application tests — Create Order

1. Customer can create an Order from their own active Cart.
2. Empty Cart is rejected.
3. Cart owned by another Customer is rejected.
4. Inactive Store is rejected.
5. Unavailable Product is rejected.
6. Server recalculates pricing; client totals are not authoritative.
7. Product name, variant, unit price and quantity are snapshotted into OrderItems.
8. Order contains exactly the Cart's Store.
9. Create Order persists Order + OrderItems atomically.
10. Successful Create Order returns a customer-facing Order number.
11. Same idempotency key + same request fingerprint returns the original result.
12. Same idempotency key + different request fingerprint is rejected.
13. Concurrent same-key requests converge to one Order.
14. Authorization prevents a Customer from creating an Order from another Customer's Cart.

### Merchant command tests

1. Merchant can Accept only an Order belonging to its Store.
2. Accept is valid only from PENDING_STORE_CONFIRMATION.
3. Prepare is valid only from ACCEPTED.
4. Ready is valid only from PREPARING.
5. Invalid transitions are rejected.
6. Every successful transition records OrderStatusHistory.
7. Unauthorized merchant access is rejected.

### Delivery boundary tests

1. READY_FOR_PICKUP does not mutate Delivery state inside the Order aggregate.
2. Delivery starts at UNASSIGNED.
3. Driver assignment changes Delivery, not Order fulfillment state.
4. Pickup changes Delivery only.
5. Out-for-delivery changes Delivery only.
6. Delivery completion changes Delivery only.
7. Delivery failure changes Delivery only.
8. Customer order read model may compose Order + Delivery status without creating duplicate mutable Order state.

## Required Failure/Concurrency Cases

- persistence failure during Order creation leaves no partial commercial Order.
- idempotency record and Order creation follow a consistency strategy that prevents duplicate Orders.
- concurrent Cart/Order changes are handled according to the persistence concurrency design.
- retries do not create duplicate OrderItems or duplicate commercial effects.

## RED Acceptance

The test suite must initially fail for the intended missing production behavior.

A RED test is valid only when:
- the failure is caused by missing/unimplemented behavior;
- the assertion represents a committed business rule;
- the test does not encode an unresolved design choice.

## Status Note and Current Use

The instruction above describes the original TDD sequence and is historical, not the current implementation task. The Create Order domain/application implementation and PostgreSQL persistence path have later CI evidence recorded in `docs/08-FOUNDATION-REVIEW-CHECKLIST.md` and `docs/06-PERSISTENCE-RELIABILITY-DESIGN-GATE.md`. Keep this RED set as the acceptance checklist; do not claim every case in this document is covered solely because the original suite passed.

The current engineering boundary is the API/security contract gate. HTTP adapter/controller work remains blocked until `docs/07-API-SECURITY-DESIGN-GATE.md` is closed. The pilot wedge remains a separate blocker for Phase B product implementation.
