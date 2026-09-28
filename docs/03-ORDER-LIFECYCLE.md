# Order Lifecycle

## Purpose
The Order lifecycle describes the commercial state of a customer purchase. Payment and Delivery are separate lifecycles.

## States

CREATED
-> PENDING_STORE_CONFIRMATION
-> ACCEPTED
-> PREPARING
-> READY_FOR_PICKUP
-> DRIVER_ASSIGNED
-> PICKED_UP
-> OUT_FOR_DELIVERY
-> DELIVERED

Exception states:
- REJECTED
- CANCELLED
- DELIVERY_FAILED
- RETURNED

## Rules

- State transitions are explicit commands.
- Invalid transitions are rejected.
- Every transition creates OrderStatusHistory.
- History records actor type/id, timestamp, previous state and new state.
- Customer cannot perform merchant-only transitions.
- Merchant can only transition Orders belonging to its Stores.
- Driver transitions require an authorized Delivery relationship.
- Delivery completion does not silently rewrite Payment state.
- Retried sensitive commands must not create duplicate effects.

## Transition Responsibility

| Transition | Actor | Command |
|---|---|---|
| CREATED -> PENDING_STORE_CONFIRMATION | System | Create Order |
| PENDING_STORE_CONFIRMATION -> ACCEPTED | Merchant | Accept |
| PENDING_STORE_CONFIRMATION -> REJECTED | Merchant | Reject |
| ACCEPTED -> PREPARING | Merchant | Prepare |
| PREPARING -> READY_FOR_PICKUP | Merchant | Ready |
| READY_FOR_PICKUP -> DRIVER_ASSIGNED | System/Admin | Assign Delivery |
| DRIVER_ASSIGNED -> PICKED_UP | Driver | Pickup |
| PICKED_UP -> OUT_FOR_DELIVERY | Driver/System | Start Delivery |
| OUT_FOR_DELIVERY -> DELIVERED | Driver | Deliver |

Cancellation and failure transition policy remains OPEN until explicitly decided.

## Verification

The transition table must become executable domain/application tests before M0 completion.
