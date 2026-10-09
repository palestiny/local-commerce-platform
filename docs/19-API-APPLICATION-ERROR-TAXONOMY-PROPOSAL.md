# API Application Error Taxonomy — Proposal

Status: **ERROR-CONTRACT DIRECTION APPROVED — INCREMENTAL MIGRATION IN PROGRESS; HTTP IMPLEMENTATION STILL BLOCKED**

## Purpose

Define stable application failures that an HTTP adapter can map to RFC 9457 Problem Details without parsing exception messages. This proposal is deliberately transport-neutral and does not select an authentication provider, controller framework, or deployment-specific limits.

## Recommended contract

Use a stable string `code` as the machine contract. Human-readable `title`/`detail` may change and must never drive client behavior.

Problem Details response extension:

- `type`: stable problem type URI (or a documented stable URN if no public URI is hosted yet).
- `title`: short human-readable summary.
- `status`: HTTP status.
- `detail`: safe, non-sensitive explanation.
- `instance`: request path/identifier where appropriate.
- `code`: stable application error code.
- `traceId`: server-generated/current request trace identifier.
- `errors`: optional field-keyed validation details.

Do not return exception type names, stack traces, SQL/provider messages, connection strings, or raw idempotency keys.

## Proposed error-code catalogue

| Code | Meaning | HTTP | Retry guidance |
|---|---|---:|---|
| `request.invalid` | Malformed request or invalid transport field | 400 | Fix request |
| `request.payload_too_large` | Request exceeds configured body limit | 413 | Reduce payload |
| `authentication.required` | Credentials absent | 401 | Authenticate |
| `authentication.invalid` | Credentials invalid/expired/revoked | 401 | Re-authenticate |
| `authorization.forbidden` | Actor lacks permission for this operation | 403 | Do not retry unchanged |
| `resource.not_found` | Resource absent or intentionally concealed as inaccessible | 404 | Verify identifier/access |
| `order.invalid_state` | Order command conflicts with current Order lifecycle | 409 | Refresh state; decide next action |
| `delivery.invalid_state` | Delivery command conflicts with current Delivery lifecycle | 409 | Refresh state; decide next action |
| `cart.not_checkoutable` | Cart missing, inactive, empty, or no longer valid for checkout | 409 | Refresh cart |
| `catalog.item_unavailable` | Product/store/variant cannot currently be ordered | 409 | Refresh catalog/cart |
| `idempotency.key_required` | Required Idempotency-Key is absent/invalid | 400 | Supply a valid key |
| `idempotency.key_reused` | Same actor/operation/key has a different request fingerprint | 409 | Use a new key only for a genuinely new operation |
| `idempotency.result_unavailable` | An explicit, trustworthy in-progress signal proves the matching operation is still executing and cannot yet replay | 409 (only when proven) | Retry same key according to the documented policy |
| `rate_limit.exceeded` | Request limit exceeded | 429 | Respect Retry-After when present |
| `dependency.unavailable` | A required dependency is temporarily unavailable and failure is safe to expose as transient | 503 | Retry only according to policy |
| `internal.unexpected` | Unexpected failure without a safe public classification | 500 | Do not blindly retry non-idempotent work |

### Classification rules

1. The same error code must mean the same thing across endpoints.
2. Do not classify by matching exception message text.
3. Do not map every application rejection to 400. Distinguish invalid input, missing resource, authorization, and state conflict.
4. A persisted `Reserved` idempotency record alone does not prove an operation is still running. Without a trustworthy lease/heartbeat or equivalent in-progress signal, classify it as `internal.unexpected`; reserve `idempotency.result_unavailable` for a proven in-progress operation.
5. Resource ownership concealment must deliberately use the same public response as missing resources where the approved security policy requires it.
6. A unique database constraint violation is not automatically a public 500. Translate only known constraints at the persistence/application boundary into the relevant stable conflict code; unknown database errors remain internal failures.
7. Cancellation, delivery completion, and other commercial mutations must preserve transaction rollback and idempotency behavior when a failure is returned.
8. Cancellation caused by caller-requested cancellation/timeouts must not be accidentally translated into `internal.unexpected`.
9. Log the stable code and trace ID; redact credentials and Idempotency-Key. Keep diagnostic exception details only in appropriately protected server-side telemetry.

## Recommended implementation design

**Recommendation: explicit typed application failures, with a narrow adapter mapping.**

- Introduce a transport-neutral `ApplicationErrorCode` catalogue and a typed `ApplicationFailureException` (or equivalent result type if preferred after codebase review).
- Each application handler raises a specific code at the point where it knows the business meaning. Do not infer meaning from its message.
- Domain transition failures should expose a stable domain rule identifier/code as well as a human-readable message; application handlers may translate known domain codes into application-level codes.
- Keep ASP.NET Core/Problem Details types out of Domain and Application.
- Add one HTTP-boundary mapper that maps explicit codes to status and safe Problem Details. Unknown exceptions map to `internal.unexpected`.
- Preserve the original exception as an inner exception for server-side diagnostics, not as a public response.
- Prefer stable constants/enums over arbitrary free-form strings at call sites.

A shared base type is recommended over a large set of near-identical rejection exceptions, but do not perform a blind global replacement: first inventory every thrown/caught exception and test assertion, then migrate by handler with tests.

## Current codebase observations (verified from the feature branch)

- `ApplicationFailureException` and `ApplicationErrorCodes` now provide a shared transport-neutral code contract.
- Create Order, Cancel Order, Get Customer Order Details, and the current Delivery command handlers derive their operation-specific rejection exceptions from the shared failure type.
- Domain transition failures in the migrated commands are translated at the application boundary to explicit stable codes.
- Focused application tests assert representative codes, including resource-not-found concealment, invalid input, state conflicts, authorization, idempotency fingerprint conflicts, and corrupt/incomplete idempotency records.
- The taxonomy is a useful baseline, but the HTTP adapter does not exist yet. Remaining risks include unclassified branches in individual handlers, unexpected infrastructure failures, cancellation propagation, and code-to-status mapping consistency. These must be audited and tested before controllers are exposed.

## Required tests before controller implementation

- Unit tests assert stable codes for representative validation, not-found, authorization, invalid-state, and idempotency-fingerprint failures.
- Mapping tests assert code-to-status mapping without inspecting exception messages.
- Unknown exceptions return the generic internal code and do not leak exception details.
- Problem Details includes the current trace ID and stable code.
- Customer ownership concealment yields the approved same public status/code as missing Order.
- Tests prove rollback remains intact when a typed failure occurs after transaction/reservation begins.
- Integration tests verify idempotent replay and fingerprint conflict at the HTTP boundary once the HTTP slice is approved.

## Owner-approved decisions

The owner approved the following implementation direction:

1. Use a shared typed application failure with stable codes rather than parsing exception messages.
2. Keep Cart checkoutability and catalog item availability as separate error codes.
3. Use `409 Conflict` only when reliable state proves a matching idempotency operation is still in progress; a persisted `Reserved` record by itself is insufficient. Treat unproven reservations and incomplete/corrupt persisted records as internal integrity failures, not as automatic transient `503` responses.

The shared `ApplicationFailureException` / `ApplicationErrorCodes` contract and focused tests are in place. The current feature branch migrates stable codes through Create Order, Cancel Order, Get Customer Order Details, and the Delivery command handlers: Ready Order for Delivery, Assign Driver, Confirm Pickup, Start Delivery, Complete Delivery, Fail Delivery, and Create Replacement Delivery. Their application tests assert representative codes for invalid input/state, not-found, authorization, idempotency fingerprint conflicts, and corrupted/incomplete idempotency results. Domain transition failures are translated at the application boundary for these migrated commands.

A follow-up test commit asserts `resource.not_found` for a missing customer Order and `request.invalid` for empty identifiers in the customer order-details query. CI run 37867134802 completed successfully on the latest commit. This does not prove every branch is covered or that unexpected infrastructure exceptions are all normalized; perform a final error-path audit as part of the HTTP adapter design. HTTP Problem Details mapping and adapter-level tests do not exist yet, and controllers remain blocked.

## Remaining decisions

The exact route DTO fields/status refinements, authentication provider and lifecycle, dispatch/admin permission matrix, idempotency header character/length boundary, rate limits, body limits, timeout budgets, and initial HTTP slice remain open. No controller implementation is authorized until those contract decisions and the required error mapping/tests are closed.


## Recommended first HTTP slice (proposal; not yet owner-approved)

Prefer a customer-only first vertical slice, limited to:

1. `POST /api/v1/orders` — create an Order from an owned Cart.
2. `GET /api/v1/orders/{orderId}` — read the customer's Order and optional active Delivery summary.
3. `POST /api/v1/orders/{orderId}/cancel` — cancel an eligible Order under the existing application rules.

Why this slice: it validates authenticated customer identity, ownership concealment, command idempotency, safe Problem Details, and the read/write boundary without prematurely choosing merchant/driver permission infrastructure. It must not ship with anonymous access or a client-supplied customer identity treated as authoritative.

### Adapter mapping proposal

- `request.invalid` → 400
- `authentication.required`, `authentication.invalid` → 401
- `authorization.forbidden` → 403
- `resource.not_found` → 404
- `order.invalid_state`, `delivery.invalid_state`, `cart.not_checkoutable`, `catalog.item_unavailable`, `idempotency.key_reused` → 409
- `request.payload_too_large` → 413
- `rate_limit.exceeded` → 429
- `dependency.unavailable` → 503 only when a known transient dependency failure is classified explicitly
- `internal.unexpected` and every unknown code/exception → 500 with a generic safe detail

Do not map `idempotency.result_unavailable` to 409 unless a future implementation can prove active execution. Current persisted reservations without such proof remain `internal.unexpected`.

### Required adapter tests

- Every known code maps to the expected HTTP status without message matching.
- Unknown code and untyped exception return generic 500 without exposing exception message/type/stack.
- Problem Details includes `code` and request `traceId`; validation `errors` is omitted when empty.
- Missing Order and inaccessible Order have the same public status and code.
- Missing/invalid auth is rejected before handler execution; customer identity comes from the authenticated principal.
- Missing/invalid/overlong idempotency keys are rejected at the boundary according to the finally approved key contract.
- Cancellation tokens propagate and are not converted into generic application failures.
- Integration tests cover idempotent replay, key/fingerprint conflict, and transaction rollback.
