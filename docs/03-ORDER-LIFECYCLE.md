# Order Lifecycle

## Purpose
The Order lifecycle describes the commercial state of a customer purchase. Payment and Delivery are separate lifecycles.

## States

CREATED
-> PENDING_STORE_CONFIRMATION
-> ACCEPTED
-> PREPARING
-> READY_FOR_PICKUP

Commercial exception states:
- REJECTED
- CANCELLED

Delivery fulfillment state is **not owned by Order**. After READY_FOR_PICKUP, fulfillment is represented by the Delivery aggregate.

## Rules

- State transitions are explicit commands.
- Invalid transitions are rejected.
- Every Order transition creates OrderStatusHistory.
- History records actor type/id, timestamp, previous state and new state.
- Customer cannot perform merchant-only transitions.
- Merchant can only transition Orders belonging to its Stores.
- Delivery commands require an authorized Delivery relationship.
- Delivery completion does not silently rewrite Payment state.
- Retried sensitive commands must not create duplicate effects.
- Order must never independently transition DRIVER_ASSIGNED, PICKED_UP, OUT_FOR_DELIVERY or DELIVERED.

## Transition Responsibility

| Transition | Actor | Command |
|---|---|---|
| CREATED -> PENDING_STORE_CONFIRMATION | System | Create Order |
| PENDING_STORE_CONFIRMATION -> ACCEPTED | Merchant | Accept |
| PENDING_STORE_CONFIRMATION -> REJECTED | Merchant | Reject |
| ACCEPTED -> PREPARING | Merchant | Prepare |
| PREPARING -> READY_FOR_PICKUP | Merchant | Ready |
| Any valid cancellable Order state -> CANCELLED | Authorized actor | Cancel |

## Delivery Boundary

Once an Order reaches READY_FOR_PICKUP, the Delivery aggregate owns:

UNASSIGNED
-> ASSIGNED
-> PICKED_UP
-> OUT_FOR_DELIVERY
-> DELIVERED

Delivery exception:
- FAILED

The customer-facing Order response may include a composed/derived DeliveryStatus, but that value is not an independently mutable Order state.

## Cross-Aggregate Operations

The application layer coordinates business operations that span Order and Delivery. Aggregate ownership remains separate.

Examples:
- READY_FOR_PICKUP may trigger Delivery creation through an application-level operation.
- Driver assignment changes Delivery only.
- Pickup, transit and completion change Delivery only.
- Customer order views compose Order + Delivery state when delivery progress is needed.

The exact transaction/event mechanism is an implementation detail to be finalized in the M0 application/persistence design without introducing a second fulfillment source of truth.

## Verification

The transition table must become executable domain/application tests before M0 completion.
