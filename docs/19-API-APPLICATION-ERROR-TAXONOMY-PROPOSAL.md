# API Application Error Taxonomy — Proposal

Status: **PROPOSED — OWNER REVIEW REQUIRED; NO HTTP IMPLEMENTATION AUTHORIZED**

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
| `idempotency.result_unavailable` | A matching operation cannot yet safely replay a completed result | 409 (provisional) | Retry same key after documented delay |
| `rate_limit.exceeded` | Request limit exceeded | 429 | Respect Retry-After when present |
| `dependency.unavailable` | A required dependency is temporarily unavailable and failure is safe to expose as transient | 503 | Retry only according to policy |
| `internal.unexpected` | Unexpected failure without a safe public classification | 500 | Do not blindly retry non-idempotent work |

### Classification rules

1. The same error code must mean the same thing across endpoints.
2. Do not classify by matching exception message text.
3. Do not map every application rejection to 400. Distinguish invalid input, missing resource, authorization, and state conflict.
4. Resource ownership concealment must deliberately use the same public response as missing resources where the approved security policy requires it.
5. A unique database constraint violation is not automatically a public 500. Translate only known constraints at the persistence/application boundary into the relevant stable conflict code; unknown database errors remain internal failures.
6. Cancellation, delivery completion, and other commercial mutations must preserve transaction rollback and idempotency behavior when a failure is returned.
7. Cancellation caused by caller-requested cancellation/timeouts must not be accidentally translated into `internal.unexpected`.
8. Log the stable code and trace ID; redact credentials and Idempotency-Key. Keep diagnostic exception details only in appropriately protected server-side telemetry.

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

- `DomainRuleViolationException` currently carries only a message.
- Application handlers use operation-specific rejection exceptions whose public meaning is currently encoded primarily in message text.
- The API gate already forbids exception-message parsing, but a complete stable application error taxonomy is not yet implemented.
- Therefore the API gate is not ready for controller implementation solely because the route direction was approved.

## Required tests before controller implementation

- Unit tests assert stable codes for representative validation, not-found, authorization, invalid-state, and idempotency-fingerprint failures.
- Mapping tests assert code-to-status mapping without inspecting exception messages.
- Unknown exceptions return the generic internal code and do not leak exception details.
- Problem Details includes the current trace ID and stable code.
- Customer ownership concealment yields the approved same public status/code as missing Order.
- Tests prove rollback remains intact when a typed failure occurs after transaction/reservation begins.
- Integration tests verify idempotent replay and fingerprint conflict at the HTTP boundary once the HTTP slice is approved.

## Decisions still required from the owner

1. Accept or revise the code catalogue and HTTP mapping above.
2. Decide whether `idempotency.result_unavailable` should be `409` or a transient `503`; recommendation is `409` only when the same operation is demonstrably still in progress, otherwise treat unexpected persisted/incomplete records as an internal integrity failure.
3. Confirm whether unavailable catalog/cart conditions should be grouped as `409` conflicts or split into more granular stable codes.
4. Approve the implementation migration approach: shared typed failure base + explicit codes (recommended), versus typed result objects.

No provider, role matrix, rate threshold, payload limit, or controller scope is decided by this proposal.
