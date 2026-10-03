# API & Security Design Gate

Status: DESIGN REVIEW

## Objective

Define stable API semantics and security boundaries before exposing production endpoints.

## API Contract Rules

Public API prefix:

/api/v1

Rules:

- DTOs are transport contracts; domain entities never become public API models.
- Commands use intention-revealing endpoints.
- Query endpoints are read-oriented.
- Clients do not submit arbitrary Order/Delivery status values.
- Validation errors use a standard machine-readable error shape.
- Every response that represents a failed request includes a correlation/trace identifier when available.
- HTTP semantics must distinguish validation, authentication, authorization, not-found, conflict and server/provider failures.

Exact response schemas are deferred to the API Design Gate implementation phase.

## Authorization

Authentication answers "who".

Authorization answers "what may this actor do to this resource".

Required resource checks:

- Customer can access only owned carts, addresses and orders.
- Merchant can operate only its stores and their orders.
- Driver can operate only deliveries assigned to that driver, except explicitly authorized operational actions.
- Admin permissions are explicit and auditable.

Role checks alone are insufficient for resource ownership.

## Authentication

The provider/framework remains OPEN.

Regardless of provider:

- credentials must be securely stored/handled
- sessions/tokens must have defined expiry and revocation behavior
- secrets must not be committed
- sensitive endpoints must require authentication
- privileged operations require authorization checks at the application boundary

## Input & Abuse Protection

M0 baseline:

- server-side validation
- payload size limits
- rate limiting for authentication and sensitive mutation endpoints
- safe error messages without secret/internal data leakage
- parameterized database access
- controlled file upload validation when product/store images are introduced

## Audit

Audit critical administrative and commercial actions, including:

- merchant/store activation changes
- order rejection/cancellation
- delivery assignment changes
- payment state changes
- privileged configuration changes

Audit records should identify actor, action, resource, timestamp and correlation context.

## Security Verification

Before pilot readiness verify:

- broken-object-level authorization attempts
- role escalation attempts
- duplicate/replayed sensitive requests
- invalid state transitions
- malformed input
- secret leakage in logs/errors
- unauthorized file/resource access

Security is part of Definition of Done, not a final cleanup phase.
