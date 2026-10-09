# Foundation Review Checklist

Status: **CURRENT BOUNDARY: API CONTRACT/SECURITY CLOSURE; HTTP IMPLEMENTATION BLOCKED**

> Current summary: the documented M0 persistence and defined M1 Delivery concurrency scenarios have CI evidence in the sections below. The next engineering boundary is API/security contract closure, followed by HTTP adapter implementation only after its gate passes. Pilot wedge decisions remain OPEN and block Phase B product implementation. Historical "next boundary" notes later in this document are retained as history, not as current instructions.

## Review Scope

| Area | Status | Evidence |
|---|---|---|
| Product/domain model | PASS | 02-DOMAIN-ARCHITECTURE-DESIGN-GATE.md |
| Module boundaries | PASS | 05-MODULE-BOUNDARIES.md |
| Order lifecycle | PASS | 03-ORDER-LIFECYCLE.md + ADR-007 |
| Payment lifecycle | PASS | Separate Payment lifecycle defined |
| Persistence model | PASS WITH OPEN IMPLEMENTATION DETAILS | 06-PERSISTENCE-RELIABILITY-DESIGN-GATE.md |
| Idempotency | PASS — POSTGRESQL PERSISTENCE VERIFIED | ADR-010 + CI run `36903751445` |
| API direction | PASS WITH CONTRACTS OPEN | 07-API-SECURITY-DESIGN-GATE.md |
| Authorization | PASS | Resource-level ownership rules defined |
| Security baseline | PASS WITH PROVIDER OPEN | 07-API-SECURITY-DESIGN-GATE.md |
| Testing strategy | PASS | 04-TESTING-STRATEGY.md |
| Pilot/product decisions | OPEN BY DESIGN | 01-PROJECT-FOUNDATION.md / 14-OPEN-QUESTIONS.md |

## Resolved Blocking Decision

### Order vs Delivery state authority

ADR-007 is ACCEPTED with **Option A — Strict separation**.

- Order owns commercial state through READY_FOR_PICKUP.
- Delivery owns assignment and fulfillment state.
- Delivery is the sole mutable source of truth for driver/pickup/transit/completion state.
- Customer-facing order views may compose Order + Delivery without duplicating mutable fulfillment state.

This removes the previous overlap between Order and Delivery.

## Foundation Exit Criteria

- One state authority is documented: PASS.
- Lifecycle transition tables are consistent: PASS.
- RED tests are the next verification artifact: READY.
- Aggregate transaction/concurrency rules are implementable: PASS WITH M0 IMPLEMENTATION DETAIL OPEN.
- API contract can be derived without ambiguous state ownership: PASS.
- No production feature code depends on unresolved domain semantics: PASS.

## Executable Foundation Evidence

### Domain GREEN

- Domain invariants and commercial lifecycle are implemented.
- CI verified the Domain test suite with **8 passed**.

### Application GREEN

- Create Order orchestration is implemented.
- CI verified the Application suite with **12 passed**.
- Full solution CI verified **20 passed, 0 warnings, 0 errors** on commit `8b9bc4662d113671cf9c43b389ce36d65106237d`.

## Verified Persistence Evidence

- Real PostgreSQL integration suite: **5/5 passed** in CI run `36473596710`.
- Concurrency and rollback scenarios are covered by the integration suite.
- Migration/initialization path is version-controlled and repeatable.
- PostgreSQL backup/restore smoke test: **passed** in CI run `36474156793`, job `109103551191`.

## Verified M1 Delivery Evidence

- Delivery domain lifecycle implementation is GREEN VERIFIED in CI.
- First M1 application slice (`PREPARING -> READY_FOR_PICKUP` + Delivery creation) is GREEN VERIFIED.
- Driver Assignment is GREEN VERIFIED.
- Pickup is GREEN VERIFIED.
- Start Delivery is GREEN VERIFIED.
- Complete Delivery is GREEN VERIFIED.
- Delivery Failure is GREEN VERIFIED.
- Replacement Delivery is GREEN VERIFIED.
- Cancellation coordination is GREEN VERIFIED.
- API/HTTP implementation remains intentionally deferred.

## M1 Delivery Design Gate

- **Status:** ACCEPTED — implementation increments verified through cancellation.
- Delivery ownership remains separate from Order commercial lifecycle.
- READY_FOR_PICKUP + Delivery creation uses one application-level PostgreSQL transaction.
- FAILED is terminal per Delivery attempt; replacement Delivery is explicit.
- Pre-pickup cancellation coordinates Order + active Delivery.
- Post-pickup ordinary customer/merchant cancellation is rejected.
- Minimal Driver eligibility is accepted for M1.
- Customer read is a composed Order + Delivery response without duplicated mutable state.
- Delivery history is append-only.

## Current M1 Execution Status — 2026-09-30

### Driver Assignment

- **GREEN VERIFIED**
- CI run `36596803406` (#152), commit `beddffa6a373ee0f59d8a98bd604354ccc97759b`, job `109503704013`.
- `Test` and `Migration and recovery smoke test` both passed.

### Pickup

- **GREEN VERIFIED**
- CI run `36599194263` (#164), commit `8e3d8788724f30b0c0d4d1f505df03dbf9aa7bfd`, job `109511861790`.
- `Test` and `Migration and recovery smoke test` both passed.

### Start Delivery

- **GREEN VERIFIED**
- CI run `36729575121` (#171), commit `2dcc22349baee9918e39305453f7b656202f55ee`, job `109935288931`.
- `Test` and `Migration and recovery smoke test` both passed.

### Complete Delivery

- **GREEN VERIFIED**
- CI run `36737706814` (#178), job `109963659983`.
- `Test` and `Migration and recovery smoke test` both passed.

### Delivery Failure

- **GREEN VERIFIED**
- CI run `36738000967` (#185), job `109964662755`.
- `Test` and `Migration and recovery smoke test` both passed.

### Replacement Delivery

- **GREEN VERIFIED**
- CI run `36739367541` (#191), job `109969385152`.
- `Test` and `Migration and recovery smoke test` both passed.

### Cancellation Coordination

- **GREEN VERIFIED**
- Latest verified commit: `ec4b1d2baa020c0fc85e224259976505758aa5a6`.
- Push CI run `36743454949` (#205), job `109983538827`.
- Pull-request CI run `36743462378` (#206), job `109983563672`.
- Both runs completed successfully; `Test` and `Migration and recovery smoke test` passed in both.
- Coverage includes pre-pickup coordinated cancellation, post-pickup rejection, no-Delivery cancellation, idempotent replay, fingerprint conflict, and unit-of-work failure boundary.
- Domain cancellation distinguishes `CANCELLED` from `FAILED`.

### Verification Boundary

The historical cancellation result below was initially verified with test doubles. Subsequent PostgreSQL integration coverage now verifies cancellation contention and the cancellation-versus-replacement race; see the PostgreSQL concurrency verification section below.

## Remaining M1 Work

1. API contract/security design and HTTP implementation.
2. Operational control and pilot-readiness concerns.

## Next Step

Resolve the API contract/security design gate, then implement HTTP endpoints only against approved contracts and resource-level authorization rules. Keep pilot-specific product and infrastructure decisions explicitly open.


### Customer Composed Read

- **GREEN VERIFIED**
- Commit `e630684b46b8b84dd7674a948db6444d3785519a`.
- CI run `36785578476` (#220), job `110126060648`.
- `Test` and `Migration and recovery smoke test` both passed.
- Verification covers customer ownership, optional Delivery composition, missing Order behavior, and non-mutating read semantics.
- Verification boundary at the time: application/domain test doubles. Later PostgreSQL persistence and concurrency evidence is recorded below.

## Historical Next Boundary — Superseded

At the time this note was written, Delivery persistence + append-only DeliveryStatusHistory was the next implementation boundary. The later sections in this checklist record that persistence work and the defined PostgreSQL concurrency scenarios as completed with CI evidence.


### Delivery Persistence + History

- **GREEN VERIFIED**
- Commit `e3cc70567db20e0e051d01128eb569d9c282fcb4`.
- CI run `36822196856` (#240), job `110239982378`.
- `Test` and `Migration and recovery smoke test` both passed.
- Delivery current-state persistence and reload verified.
- DeliveryStatusHistory append/read verified with transition metadata.
- PostgreSQL partial unique index verified by rejecting a second active Delivery for the same Order.
- Database trigger enforces append-only DeliveryStatusHistory by rejecting UPDATE/DELETE.
- Verification boundary at the time: basic persistence only. End-to-end command concurrency was subsequently verified as recorded below.

## Historical Next Boundary — Superseded

PostgreSQL concurrency verification for the defined M1 scenarios was subsequently completed; see the verification evidence below.


## PostgreSQL Concurrency Strategy

- **ADR-009 ACCEPTED:** PostgreSQL row-level locking for M1 Delivery mutation commands.
- Mutable Delivery command loads inside a transaction use `SELECT FOR UPDATE`.
- A real PostgreSQL test verifies the second transactional load waits for the first transaction and reads the committed Delivery state.
- **GREEN VERIFIED for the defined M1 scenarios:** end-to-end command concurrency, cancellation contention, replacement-delivery contention, and PostgreSQL idempotency persistence. See ADR-009 and ADR-010 for exact scope and limitations.

## Current Next Boundary

The transaction-boundary refactor and real PostgreSQL concurrency verification are complete for the scenarios listed below. Next: close the remaining API contract/security decisions and adapter tests. HTTP implementation remains blocked until that gate passes.

### PostgreSQL concurrency verification

**Current status: GREEN VERIFIED for the defined M1 concurrency scenarios.**

Verified after commit `e2bc8cf85878510f13dd5697a909b4876af98a10`:
- CI run #264 (`36825487024`) completed successfully.
- Delivery persistence test suite verifies a second PostgreSQL transaction waits while the first transaction holds the Delivery row lock, then observes the committed state.
- Delivery mutation handlers now load mutable Delivery state inside the active Unit of Work for AssignDriver, ConfirmPickup, StartDelivery, CompleteDelivery, and FailDelivery.

The above historical gaps were subsequently closed for the defined M1 scenarios by CI run `37861612028`, commit `fdba8b7524b8550ef707fc80780327c9a8dce189`. Coverage includes competing driver assignment, replacement creation, concurrent cancellation, cancellation-versus-replacement, persisted Order eligibility under lock, and competing ConfirmPickup/StartDelivery/CompleteDelivery/FailDelivery commands with operation-specific loser exceptions. Idempotency persistence and migration/recovery smoke tests also passed. This is not exhaustive proof of every possible cross-command interleaving or production-load behavior.


## ADR-010 Acceptance Boundary

**Status: ACCEPTED — PostgreSQL persistence GREEN VERIFIED**

Generalized command idempotency is implemented as a resource-neutral persistence capability. CI run `37861612028` passed the idempotency persistence tests and migration/recovery smoke test alongside the defined M1 Delivery concurrency suite. The implementation is verified for the exercised reservation/replay/fingerprint/transaction/rollback scenarios; future commands must add concurrency tests for their own mutation paths.
