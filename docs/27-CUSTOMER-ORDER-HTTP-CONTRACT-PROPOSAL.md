# Customer Order HTTP Contract Proposal

Status: **DRAFT FOR OWNER REVIEW — NOT APPROVED FOR IMPLEMENTATION**

Date: 2026-10-16

This proposal narrows review to the owner-approved customer Order routes. It does not select an identity provider, authorize controller implementation, or assert that the current application layer already supports every proposed wire field.

## Non-negotiable boundaries

- Only these routes are in scope:
  - `POST /api/v1/orders`
  - `GET /api/v1/orders/{orderId}`
  - `POST /api/v1/orders/{orderId}/cancel`
- Actor identity comes from the authenticated server-side principal. No request field, query parameter, or header supplied by a client may override it.
- Do not expose Domain entities directly. Use explicit transport DTOs.
- A customer's ownership of an Order/Cart is a resource-level check, not a role-only check.
- Use stable application error codes; never map errors by matching exception-message text.
- Error responses use RFC 9457 Problem Details with a stable application `code` and a `traceId` when available. Never expose stack traces, SQL/provider details, tokens, secrets, or raw idempotency keys.
- All timestamps, if included, are UTC ISO 8601.
- These contracts remain proposals until the owner approves unresolved choices and the API/security gate exit evidence is complete.

## Route proposal

| Route | Request proposal | Success proposal | Authorization / key rules |
|---|---|---|---|
| `POST /api/v1/orders` | JSON body identifies the customer's cart; do not accept authoritative customer ID, order status, prices, or totals from the client. Require `Idempotency-Key`. | `201 Created`; explicit response DTO containing the created order's public identifier and order number; `Location` points to the created Order resource. | Authenticated customer only; verify cart ownership against the principal before creation. |
| `GET /api/v1/orders/{orderId}` | Order identifier in the route only; no customer/actor ID in query/body. | `200 OK`; explicit Order details DTO. Include delivery summary only if product/domain review confirms it is part of the first customer contract. | Authenticated customer must own the Order. Recommend the same public `404` for missing and non-owned Orders to prevent resource enumeration. |
| `POST /api/v1/orders/{orderId}/cancel` | No arbitrary actor ID or client-selected Order status. Cancellation reason is OPEN; do not invent a body field until the application contract is reviewed. Require `Idempotency-Key`. | Working baseline `200 OK`; response body and replay representation remain OPEN. | Authenticated owning customer only; lifecycle rules determine cancellation eligibility. Recommend concealed `404` for non-owned/missing Orders, consistent with GET. |

## Proposed Idempotency-Key contract

Working proposal, subject to owner review:
- Required on the two state-changing routes above.
- Accept 1–128 visible ASCII characters; reject missing, empty, whitespace-only, and overlong values before invoking the application handler.
- Treat keys as opaque and case-sensitive; do not trim, lowercase, or normalize them.
- Scope the idempotency record to authenticated actor + operation + key, and bind it to a deterministic request fingerprint.
- Same scope/key/fingerprint after successful completion replays the original logical result without repeating the commercial effect.
- Same scope/key with a different fingerprint returns `409 Conflict`.
- A reserved record alone is not proof of a live in-progress operation. Do not return an “in progress” response unless persistence state can prove that condition.
- Do not return the raw key in response bodies, error bodies, or logs.
- Before final approval, verify atomicity across key reservation, Order mutation, and result completion, including rollback and concurrent requests.

## Proposed public error categories (status remains to be mapped against application codes)

| Condition | Proposed public behavior | Decision/evidence still required |
|---|---|---|
| Missing/invalid authentication | `401 Unauthorized` for protected routes | Selected identity/session design and integration tests |
| Authenticated but missing/non-owned Order | Recommend indistinguishable `404 Not Found` | Owner approval and tests for GET and cancel |
| Cart not owned by customer | Safe not-found/forbidden behavior consistent with application policy; do not leak another customer's cart | Review existing application error code and owner policy |
| Invalid request fields | `400 Bad Request` with field-keyed safe validation details | Final DTO and validation boundaries |
| Same idempotency key, different fingerprint | `409 Conflict` | Application error-code mapping and tests |
| Order state disallows cancellation | `409 Conflict` proposed | Verify lifecycle/application error taxonomy and owner-approved wording |
| Unexpected exception | Generic `500 Internal Server Error` Problem Details; no internal details | Adapter test with log/error redaction |

This table is not the final error mapping. Each actual application error code must be mapped explicitly, and unknown exceptions must have a safe generic path.

## Contract tests required before implementation

### Create Order
- Missing/invalid authentication never invokes the handler.
- Client cannot choose the authoritative customer identity, Order status, price, or total.
- A cart owned by another customer cannot be ordered.
- Missing/invalid Idempotency-Key is rejected before commercial mutation.
- Same key and same request produce one commercial effect and a replayable result.
- Same key and different request fingerprint returns the approved conflict response.
- The success response, `Location`, and replay semantics match the approved contract.

### Get Order
- Owner can read their Order.
- Customer A cannot read Customer B's Order.
- Missing and non-owned Orders produce the same public response if concealment is approved.
- DTO does not expose internal entities, secrets, unrelated customer data, or unapproved delivery fields.

### Cancel Order
- Owner can cancel only when the application lifecycle allows it.
- Customer A cannot cancel Customer B's Order.
- Missing/non-owned behavior matches the approved concealment policy.
- Repeating the same idempotent cancellation does not repeat side effects.
- Conflicting request fingerprint and invalid lifecycle state map to reviewed stable errors.

### Cross-cutting
- RFC 9457 content type and envelope are stable.
- Trace identifier is propagated where available.
- No stack trace, exception message, SQL/provider detail, bearer token, or raw idempotency key is returned or logged.
- Cancellation, timeout, and persistence failure do not leave a false completed idempotency record or a partial commercial effect.
- Request body limits, timeouts, and rate limits have documented deployment-informed values before pilot.

## Owner decisions still needed

1. Approve or change the proposed `404` concealment behavior for cancellation, aligned with GET.
2. Confirm whether Order details include a delivery summary in the first contract.
3. Confirm whether cancellation accepts a customer-supplied reason; if yes, define allowed values and validation.
4. Approve final Idempotency-Key syntax and replay status/body/headers after checking the application persistence semantics.
5. Approve final public Order ID format and DTO fields only after reviewing the existing domain/application types.
6. Close authentication provider/session lifecycle and numeric operational limits separately.
7. Define the pilot wedge before Phase B; this document does not infer it.

## Stop condition

Do not implement production HTTP endpoints from this draft. Mark the API/security gate ready for owner review only after the DTOs are reconciled with real application/domain types, all error codes have explicit mappings, auth/resource checks are testable, idempotency behavior is proven, operational limits are justified, and the pilot dependency is resolved.
