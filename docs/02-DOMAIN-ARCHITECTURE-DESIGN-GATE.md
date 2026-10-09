# Domain & Architecture Foundation Design Gate

Status: READY FOR TDD RED

This gate must be completed before production feature implementation.

## Objective
Establish a stable business/domain model and technical boundary for M0 without prematurely locking pilot-specific choices.

## Committed Baseline
- Modular Monolith.
- ASP.NET Core/.NET backend.
- PostgreSQL.
- React + TypeScript for web surfaces.
- Clear domain/application/infrastructure boundaries.
- Versioned public APIs.
- API DTOs separate from domain entities.
- Logical modules: Identity, Merchant, Store, Catalog, Cart, Ordering, Delivery, Payments, Notifications, Administration.

## Domain Model

### Merchant
Business operating one or more Stores.

### Store
Customer-facing local selling point owned by a Merchant.

Lifecycle:
DRAFT -> PENDING_APPROVAL -> ACTIVE -> PAUSED/SUSPENDED -> CLOSED

### Product
MVP product owned by a Store with customer-facing identity, pricing and availability.

Lifecycle:
ACTIVE -> OUT_OF_STOCK/HIDDEN -> ARCHIVED

### Cart
Customer purchase intent.

MVP invariant:
- one active cart belongs to one Store.

### Order
Commercial snapshot plus lifecycle state.

Snapshot includes product name, variant, unit price, quantity, discount and relevant purchase-time values.

Lifecycle:
CREATED -> PENDING_STORE_CONFIRMATION -> ACCEPTED -> PREPARING -> READY_FOR_PICKUP

Commercial exception states:
REJECTED, CANCELLED

Delivery fulfillment state is owned exclusively by the Delivery aggregate after READY_FOR_PICKUP.

### Payment
Independent lifecycle:
PENDING -> PAID / FAILED / REFUNDED

MVP method: CASH_ON_DELIVERY.

### Delivery
Independent fulfillment aggregate.

Lifecycle:
UNASSIGNED -> ASSIGNED -> PICKED_UP -> OUT_FOR_DELIVERY -> DELIVERED
with FAILED as an exception path.

### CustomerAddress
Separate from GeoPoint. May contain label, recipient, phone, address text, latitude/longitude, building/floor/apartment and delivery notes.

## Core Invariants

1. Order cannot be created from an empty Cart.
2. MVP Order contains items from exactly one Store.
3. Purchased price comes from the validated checkout snapshot.
4. Unavailable products cannot be ordered.
5. Inactive Stores cannot accept new Orders.
6. Customer cannot create an Order from another Customer's Cart.
7. State transitions are explicit and valid.
8. Payment state is independent from Order state.
9. Delivery state is independent from Order state.
10. Sensitive commands are idempotent.
11. Only authorized actors can execute merchant/driver/admin commands.
12. Order history records lifecycle transitions and actor context.

## Transaction Boundaries

Create Order should validate cart ownership, Store/product availability, prices and totals, then create the Order and OrderItems within the required consistency boundary.

Merchant commands must enforce current Order state and Store ownership.

Delivery commands must enforce Delivery state and driver authorization.

Payment confirmation must be independent and idempotent.

## Idempotency

Required for:
- Create Order
- sensitive Order commands
- Delivery commands
- Payment confirmation

Scope: Actor + Operation + Key.

Same key + same request fingerprint returns the original result.
Same key + different request fingerprint is rejected.

## API Direction

Public API prefix: /api/v1

Prefer intention-revealing commands such as:
- POST /orders
- POST /orders/{id}/accept
- POST /orders/{id}/reject
- POST /orders/{id}/prepare
- POST /orders/{id}/ready
- POST /deliveries/{id}/accept
- POST /deliveries/{id}/pickup
- POST /deliveries/{id}/deliver

Exact contracts require a separate API Design Gate.

## Authorization Boundary

Customer: own profile, addresses, carts and orders.

Merchant: own Merchant/Store resources and their Orders.

Driver: assigned Delivery resources.

Admin: operational resources according to explicit permissions.

Authorization is resource-level, not role-only.

## Data Model Direction

Initial relational entities:
users, roles, user_roles, merchants, stores, store_categories, products, product_variants, product_images, customer_addresses, carts, cart_items, orders, order_items, order_status_history, deliveries, delivery_status_history, payments, notifications, notification_deliveries, audit_logs, idempotency_records.

Exact schema is NOT FINAL until persistence review.

## Failure & Recovery Baseline

For every order-critical command document validation failures, concurrency conflicts, persistence/provider failures, retry behavior, idempotency behavior, audit evidence and recovery action.

No retry may create duplicate commercial effects.

## Explicitly Not Decided Here

Pilot city/zones, merchant categories, delivery operating model, commission/fees, mobile framework, auth provider, hosting provider, maps provider, payment provider, search evolution and online payments remain OPEN unless separately decided.

## Gate Exit Criteria

- Aggregate boundaries reviewed.
- Invariants converted into tests.
- Order/Payment/Delivery transitions documented with distinct state ownership.
- Authorization boundaries verified.
- Transaction/idempotency rules implementable.
- Persistence model has no contradiction with domain rules.
- API direction approved.
- Remaining OPEN questions explicitly separated from committed architecture.

## Next Step

Foundation decision ADR-007 is accepted. Proceed to TDD RED for Create Order, while treating the remaining M0 transaction/read-model/failure semantics as implementation design work.
