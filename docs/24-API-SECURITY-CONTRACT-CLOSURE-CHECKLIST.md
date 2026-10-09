# API & Security Contract Closure Checklist

Status: **REVIEW CHECKLIST — NO NEW OWNER DECISIONS MADE — HTTP IMPLEMENTATION BLOCKED**

## Purpose and boundary

This checklist converts the remaining items in the API & Security Design Gate (docs/07-API-SECURITY-DESIGN-GATE.md) and API Application Error Taxonomy (docs/19-API-APPLICATION-ERROR-TAXONOMY-PROPOSAL.md) into explicit closure evidence. It is a review aid, not approval of unresolved choices.

Current foundation code and application-level error-code tests do not prove an HTTP API is secure or contract-complete. Do not start controllers, authentication integration, or public endpoint implementation from this checklist alone. Do not start Phase B while the pilot wedge remains open.

## Contract closure register

| ID | Area | Current baseline | Still required to close | Required evidence |
|---|---|---|---|---|
| API-01 | Initial HTTP slice | Customer Order routes are proposed as a smaller first slice; owner approval is not recorded here | Owner chooses customer-only slice or explicitly approves a broader initial route set | Recorded owner decision; route list updated consistently in the gate |
| API-02 | Route and DTO contract | /api/v1 route matrix is a design baseline; DTO fields and some success semantics remain open | Specify request/response schemas, required/optional fields, identifier formats, nullability, and success status/body/headers for every included route | Reviewed contract table or OpenAPI draft; no Domain entities exposed |
| API-03 | Identity integration | Provider/framework and token/session lifecycle are OPEN | Select an authentication integration boundary and define credential/session expiry, revocation, and failure behavior for the chosen client strategy | Owner decision and provider/framework-specific design backed by current official documentation |
| API-04 | Resource authorization | Ownership/assignment checks and concealed customer Order reads are required | Define the permission matrix for every route in the chosen slice; explicitly separate customer ownership checks from role checks | Route × actor × resource authorization matrix; negative tests for cross-owner access |
| API-05 | Idempotency header | Same actor + operation + key + fingerprint replays the completed result; fingerprint mismatch conflicts. Key reservation, mutation, and completion must preserve required atomicity | Confirm accepted character set and length boundary; define exact HTTP replay representation/status and behavior when the outcome cannot safely be replayed | Owner-approved header contract; boundary, replay, conflict, concurrency, and rollback tests |
| API-06 | Error-to-HTTP mapping | Stable transport-neutral error codes and a proposed RFC 9457 Problem Details envelope exist | Review every included operation's possible application failures and map each to status/code; specify safe generic handling for unknown failures | Explicit mapping table; adapter tests proving no message-text matching or exception-detail leakage |
| API-07 | Request validation and limits | Body limits, timeouts, and rate limits are required but numeric values are OPEN | Set deployment-informed body-size, timeout, and rate-limit values; identify configuration ownership and exceptions for legitimate workloads | Recorded rationale tied to hosting/traffic assumptions; tests for oversized payloads, timeout/cancellation, and rate limits |
| API-08 | Observability and audit | Trace ID in errors and audit of privileged/commercial transitions are required | Define which actions need durable audit records in the chosen slice and verify trace propagation and sensitive-field redaction | Audit event list/schema, trace tests, and log/error redaction tests |
| API-09 | API description and compatibility | OpenAPI review is required before pilot | Define the versioned API description and verify it matches implemented routes, DTOs, statuses, and headers | Generated/maintained OpenAPI artifact and contract consistency test or documented review |
| API-10 | Pilot scope dependency | Pilot categories, merchant count, service zones, operating split, and success thresholds remain OPEN | Resolve the pilot wedge through the product/design gate; do not infer it from the long-term ambition to support many categories | Explicit owner decision and updated project foundation/open-question/decision records |

## Required route-level review template

For every route included in the approved first slice, complete all fields before implementation:

- **Actor source:** authenticated server-side principal; no client-supplied actor ID is authoritative.
- **Resource scope:** exact ownership/relationship check and whether inaccessible resources are concealed as 404.
- **Request DTO:** fields, validation, identifier format, maximum lengths, and fields that must never be client-authoritative.
- **Success contract:** status, response schema, Location where resource creation requires it, and replay behavior.
- **Failure contract:** stable application code, HTTP status, safe Problem Details fields, and retry guidance.
- **Idempotency:** required/optional header, accepted syntax, operation scope, fingerprint semantics, replay result, conflict result, and failure/rollback behavior.
- **Abuse controls:** applicable body limit, timeout, rate-limit policy, and sensitive-data redaction.
- **Verification:** positive contract test, invalid-input test, unauthorized/cross-resource test, replay/conflict test where applicable, and unexpected-error sanitization test.

A route is not contract-complete merely because its application handler exists or its unit tests pass.

## Recommended closure order

1. Confirm the initial HTTP slice. The customer-only Order slice is a proposal, not an owner-approved decision in this checklist.
2. Resolve the authentication boundary using the selected client strategy, hosting constraints, budget, and current official provider/framework documentation. Do not assume a provider or pricing.
3. Complete the route-level DTO, authorization, success, and error contracts for only the approved slice.
4. Close the idempotency transport details and operational limits with documented rationale.
5. Define contract/security tests before controller implementation.
6. Implement the adapter only after the gate's exit criteria are met, then verify the actual HTTP boundary with integration tests.
7. Keep Phase B blocked until the separate pilot-wedge decisions are approved.

## Explicit non-decisions

This checklist does not select an authentication provider, decide web/mobile technology, define admin/dispatch permissions, set numeric rate limits/timeouts/body limits, approve exact DTO fields, choose the first HTTP slice, or approve the pilot wedge. These remain OPEN until the owner decides them and the corresponding records are updated.

## Gate exit evidence

The API/security gate may be proposed for owner review only when:

- Every route in the approved initial slice has a completed route-level contract.
- Authentication integration and resource authorization are specified and testable.
- Idempotency syntax, replay, conflict, and failure semantics are explicit.
- Every known application error code and unknown failure has safe, tested HTTP behavior.
- Abuse-control values have a documented deployment rationale.
- Contract/security test cases exist before controller work.
- OpenAPI and implementation can be compared by repeatable verification.
- No claim of pilot readiness is made solely from application-layer tests or CI for an earlier commit.

**Stop condition:** until the owner approves the remaining decisions and the required design evidence exists, do not implement HTTP controllers or authentication-provider integration, and do not mark this gate PASS.


## Application-handler contract review (feature branch evidence)

Review scope: the current Create Order, Get Customer Order Details, and Cancel Order application handlers and their focused application tests on `feature/foundation-domain-architecture`. This is an application-layer review only; it does not verify HTTP middleware, authentication, deployed authorization, or PostgreSQL concurrency.

| Operation | Verified application contract | Gap to close before HTTP |
|---|---|---|
| Create Order | `CreateOrderCommand` carries `CustomerId`, `CartId`, and `IdempotencyKey`. The handler checks Cart ownership, active/non-empty Cart, active Store, product availability and variant match; it calculates prices server-side and returns `OrderId` + `OrderNumber`. | The adapter must derive `CustomerId` from the authenticated principal, never trust a body-supplied customer ID. The handler rejects a blank key but does not enforce the proposed visible-ASCII / 1–128 character boundary; enforce the finally approved syntax at the HTTP boundary and test it. Request/response DTO and `201 Created`/Location details remain to be finalized. |
| Get Customer Order Details | `GetCustomerOrderDetailsQuery` carries `CustomerId` + `OrderId`. Missing Order and failed customer authorization both produce `resource.not_found`; focused tests assert both cases. The result contains Order ID/number/status and optional Delivery ID/status/assigned timestamp. | The adapter must derive `CustomerId` from the authenticated principal. Confirm whether this result is the intended public response shape and document nullability/timestamp representation. Application tests prove handler behavior only, not that the HTTP boundary enforces authentication. |
| Cancel Order | `CancelOrderCommand` carries `OrderId`, `ActorId`, and `IdempotencyKey`. The handler checks authorization, rejects cancellation after pickup, coordinates Order and active Delivery cancellation, and records/replays a typed idempotency result. | The adapter must derive `ActorId` from the authenticated principal. The handler distinguishes an unauthorized actor with `authorization.forbidden`, while missing Order uses `resource.not_found`; decide and document whether the customer-only cancellation route should conceal inaccessible Orders as 404 or intentionally return 403. Define the public response body/status and replay response. The unit test for transaction failure fails before the operation delegate and does not prove rollback after partial persistence writes. |

### Findings that are verified — and limits

- The three handlers use typed application failure codes rather than requiring HTTP adapters to parse message text for the reviewed branches.
- The customer order-details tests cover missing/inaccessible Order using the same `resource.not_found` code, invalid empty identifiers, and optional Delivery presence.
- The Create Order and Cancel Order result records are application contracts, not approved public DTO schemas.
- The reviewed unit tests do not establish actual HTTP authentication/authorization, Problem Details serialization, header validation, OpenAPI compatibility, or production persistence rollback/concurrency.
- No authentication provider, first HTTP slice, final DTO schema, idempotency header syntax, or cancellation concealment policy is selected by this review.

**Next closure action:** complete the route-level decisions above for the owner-approved initial slice, then add adapter contract tests before implementing controllers. Keep HTTP implementation blocked until the API/security gate exit evidence is met.
