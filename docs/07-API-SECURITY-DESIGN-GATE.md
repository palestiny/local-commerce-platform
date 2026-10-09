# API & Security Design Gate

Status: **CONTRACT PROPOSAL READY FOR OWNER REVIEW — HTTP IMPLEMENTATION BLOCKED**

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

Exact route names and DTOs below are a proposed v1 contract, not yet owner-approved. Do not implement controllers until the contract is accepted and application error semantics are mapped without parsing exception messages.

## Proposed v1 Route Matrix

| Method | Route | Application operation | Success |
|---|---|---|---|
| POST | `/api/v1/orders` | Create Order from the authenticated customer's Cart | `201 Created` |
| GET | `/api/v1/orders/{orderId}` | Customer composed Order + optional active Delivery read | `200 OK` |
| POST | `/api/v1/orders/{orderId}/cancel` | Cancel Order and coordinate pre-pickup Delivery cancellation | `200 OK` |
| POST | `/api/v1/orders/{orderId}/ready-for-pickup` | Merchant marks Order ready and creates Delivery atomically | `200 OK` |
| POST | `/api/v1/deliveries/{deliveryId}/assignment` | Assign an active Driver | `200 OK` |
| POST | `/api/v1/deliveries/{deliveryId}/pickup` | Confirm pickup | `200 OK` |
| POST | `/api/v1/deliveries/{deliveryId}/start` | Start delivery after pickup | `200 OK` |
| POST | `/api/v1/deliveries/{deliveryId}/complete` | Complete delivery | `200 OK` |
| POST | `/api/v1/deliveries/{deliveryId}/fail` | Record an explicit delivery failure | `200 OK` |

The assignment route uses an explicit command rather than a client-controlled Delivery status. The failure request carries a failure code and reason. Route naming, success payload fields, and exact DTO schemas remain subject to owner approval.

## Actor Identity and Resource Authorization

- Derive the customer, merchant, driver, or operational actor identity from the authenticated server-side principal; never trust an actor/customer ID supplied by the client as proof of identity.
- Apply authorization to the target resource and its ownership/assignment relationship, not only to a role claim.
- Customer order reads should return the same `404` response for a missing Order and an Order the customer cannot read, preventing an ownership-enumeration oracle.
- Merchant Order commands must verify the Order belongs to a Store controlled by that Merchant.
- Driver pickup/start/complete/fail commands must verify the actor is the assigned Driver, except for a separately authorized operational intervention.
- Driver assignment and administrative intervention require an explicit operational permission policy. The precise role/permission model remains OPEN.

## Idempotency Header Contract (Proposed)

Every state-changing command requires an `Idempotency-Key` request header.

- Scope the key to authenticated actor + operation; tenant scope may be added when a tenant boundary exists.
- Treat the key as opaque and case-sensitive; do not silently trim, lowercase, or otherwise normalize it.
- Proposed boundary: 1–128 visible ASCII characters; reject absent, empty, whitespace-only, or overlong values before invoking the handler.
- Same key + same canonical request fingerprint replays the original completed result without a second commercial effect.
- Same key + different fingerprint returns `409 Conflict`.
- The key reservation, business mutation, and completion record must share the established persistence transaction where that command requires atomicity.
- Never log the raw key or credentials. Logs may include operation, a safe correlation ID, and a non-reversible key digest if operationally required.

The header length/character boundary and replay response status are proposed choices that require owner approval before becoming a public compatibility promise.

## Standard Error Contract

Use RFC 9457 Problem Details as the response envelope, extended with stable machine-readable fields:

- `code`: stable application error code, independent of exception text.
- `traceId`: request trace/correlation identifier.
- `errors`: optional field-level validation details; omit when not applicable.

Proposed HTTP mapping:

| Condition | HTTP status |
|---|---:|
| Malformed JSON or invalid transport fields | `400 Bad Request` |
| Missing/invalid authentication | `401 Unauthorized` |
| Authenticated actor lacks permission for an operation | `403 Forbidden` |
| Resource absent, or intentionally concealed as inaccessible | `404 Not Found` |
| Invalid state transition, duplicate active Delivery, or idempotency fingerprint conflict | `409 Conflict` |
| Payload exceeds configured limit | `413 Content Too Large` |
| Rate limit exceeded | `429 Too Many Requests` |
| Known temporary dependency outage after safe failure handling | `503 Service Unavailable` |
| Unexpected server/provider failure | `500 Internal Server Error` |

Do not map exceptions by matching their human-readable messages. Before HTTP implementation, application failures must expose stable error types/codes or be translated by a narrow, explicit exception-to-error adapter. Responses must not expose SQL, stack traces, credentials, connection strings, or internal provider details.

## Request and Response Rules

- Create Order accepts a Cart identifier and required idempotency header; it does not accept authoritative customer identity, price, Order status, or totals from the client.
- Command bodies contain only command inputs. Clients cannot post arbitrary lifecycle status values.
- Use transport DTOs, not serialized Domain entities.
- Return the created resource identifier and stable public fields; use a `Location` header for resource-creating requests.
- Use UTC ISO 8601 timestamps for public timestamps.
- Validate request body size, identifier formats, string lengths, and required fields before invoking application handlers.
- Define pagination and filtering only for collection endpoints when they are introduced; do not invent collection APIs for the initial vertical slice.

## Abuse Protection, Observability, and Audit

- Require TLS at the deployment edge and configure trusted-proxy handling explicitly.
- Configure bounded request body sizes and request timeouts.
- Rate-limit authentication and sensitive mutations; exact thresholds must be selected from expected traffic and deployment capacity rather than guessed in this gate.
- Keep secrets out of source, responses, and logs; redact authorization and idempotency headers.
- Propagate a trace/correlation ID and include it in error responses.
- Audit privileged and commercially significant transitions with actor, action, resource, timestamp, and trace context. Do not treat an ordinary application log as an audit record.

## Required API Verification Before Pilot

- Contract tests for success DTOs, status codes, headers, and Problem Details codes.
- Authentication required on all non-public routes.
- Object-level authorization tests for cross-customer, cross-merchant, unassigned-driver, and privilege-escalation attempts.
- Idempotency tests for replay, fingerprint conflict, concurrent duplicates, and rollback.
- Invalid/terminal state transition tests.
- Malformed/oversized payload and rate-limit tests.
- Error/log redaction tests.
- OpenAPI contract reviewed against the implemented routes and DTOs.

## Open Decisions (Do Not Guess)

- Authentication provider/framework and token/session lifecycle.
- Exact permission matrix for dispatch/admin operations.
- Final request/response DTO fields and whether command successes return `200` or `204` where no representation is needed.
- Idempotency header maximum length and exact accepted character set.
- Rate-limit thresholds, payload limits, and timeout budgets based on hosting and traffic expectations.
- Whether the first HTTP slice includes only customer Order endpoints or also merchant/driver commands.

## Gate Exit Criteria

- Owner approves the v1 route matrix and DTO direction.
- Stable application error codes/types exist for validation, authorization, not-found, state conflict, idempotency conflict, and provider failures.
- Authentication integration boundary is selected without coupling Domain/Application to the provider.
- Resource-level authorization policy is implemented and testable.
- Idempotency header behavior and error/status mapping are accepted.
- API contract tests and security tests are defined before controller implementation.



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
