# Roadmap

## M0 — Core Commerce Transaction
Identity -> Store -> Product -> Cart -> Checkout -> Order -> Merchant acceptance -> Preparation -> Ready

Gate: a real order can be created and progressed through the merchant workflow with authorization, validation, idempotency, tests, and audit history.

## M1 — Delivery
Ready -> Driver assignment -> Pickup -> Out for delivery -> Delivered

Gate: a real order completes the delivery lifecycle and defined failure paths are recoverable.

## M2 — Operational Control
Admin operations for merchants, stores, drivers, orders, zones, fees, disputes, and audit.

Gate: operations can observe and intervene without direct database manipulation.

## M3 — Pilot Readiness
Security hardening, performance checks, recovery, monitoring, backups, deployment, smoke tests, and support runbook.

Gate: the system can operate a controlled pilot with known limits and documented recovery procedures.

A milestone is not complete because code exists. Its acceptance gate must be verified and documented.
