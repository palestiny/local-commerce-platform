# Verified Project Readiness Baseline

**Reviewed:** 2026-10-16  
**Repository:** `palestiny/local-commerce-platform`  
**Baseline branch:** `feature/foundation-domain-architecture`  
**Status:** Foundation exists; not pilot-ready.

This document records observable repository and CI state. It is not a claim that the product is complete.

## Verified repository facts

- `main` is the default branch and contains the README plus the initial project constitution/foundation, roadmap, decision log, open questions, and definition of done.
- `feature/foundation-domain-architecture` is the current implementation baseline. The Git comparison reported it **348 commits ahead** of `main` and zero commits behind at review time.
- The implementation baseline contains a .NET solution, Domain/Application/Infrastructure projects, PostgreSQL persistence, and automated test projects for domain, application, and infrastructure behavior.
- The workflow `.github/workflows/dotnet.yml` restores and tests the .NET 8 solution against PostgreSQL 16 and runs a database migration/recovery smoke test.
- PR #1 is open and Draft; it proposes the first domain/Create Order vertical slice and foundation. It is not merged into `main`.
- PR #14 is open and Draft; it adds replacement-delivery store-consistency and authorization-related behavior. Its description explicitly says a production authorization adapter and trusted actor-role / Merchant-to-Store membership source remain blocking. Do not expose this operation through HTTP until that gate is closed.
- PR #16 is open and Draft; it documents a proposed React Native + Expo + TypeScript direction. It does not provide a mobile runtime implementation and the stack remains a proposal until the owner accepts the gate.
- The latest visible CI run for PR #14 at review time, run [38385600936](https://github.com/palestiny/local-commerce-platform/actions/runs/38385600936), passed both `dotnet test` and the migration/recovery smoke test. An earlier run failed to compile because test call sites had not been updated for the new handler constructor; the later successful run is evidence that this specific compile failure was corrected, not proof that all security requirements are complete.

## Explicitly not verified / not ready

- No production HTTP API surface or API host has been verified in the reviewed foundation.
- HTTP Problem Details mapping and adapter-level API contract tests remain outstanding, as stated in `docs/07-API-SECURITY-DESIGN-GATE.md`.
- Production authentication, trusted actor identity, role policy, and resource-ownership authorization are not complete.
- The replacement-delivery authorization gate is not closed; test-only allow-all authorization does not prove production access control.
- No production mobile runtime, Android/iOS build, or end-to-end mobile-to-API flow has been verified.
- Deployment, production secrets, observability, backup scheduling, restore rehearsal in a target environment, support operations, and pilot acceptance have not been verified by this review.
- CI passing on a feature branch is not equivalent to a production release or a green `main`.

## Release discipline

1. Do not merge the 348-commit implementation branch into `main` as an unreviewed bulk operation.
2. Keep the current implementation baseline isolated until its complete change set, security gates, test results, and merge strategy are reviewed.
3. Keep security-sensitive operations unavailable at the transport layer until their trusted identity and resource-scope policies are implemented and tested.
4. Treat every PR as independently reviewable: verify exact base/head, latest CI, changed files, design-gate status, and unresolved risks before merging.
5. Do not delete branches or close PRs solely to reduce counts; first record whether each contains unique work, is superseded, or is safely merged.
6. Update this baseline whenever a gate changes state, and include evidence (commit, PR, CI run, or test report) rather than using unqualified words such as “complete”.

## Ordered execution plan

### Gate 1 — Establish a trustworthy integration baseline
- Review the complete branch/PR graph and identify the intended integration sequence.
- Reconcile workflow triggers so PRs targeting the chosen integration branch receive required CI checks.
- Run the full .NET/PostgreSQL suite on the exact proposed merge head.
- Keep `main` unchanged until the merge candidate has a reproducible green result.

**Exit evidence:** named candidate commit, exact test/CI run, reviewed diff, and documented merge order.

### Gate 2 — Close API/security contracts before controllers
- Finish the typed application error catalogue for every operation intended for exposure.
- Approve exact request/response DTOs, idempotency header constraints, and RFC 9457 Problem Details mappings.
- Define trusted actor identity and role/resource authorization for customer, merchant, driver, and operational actors.
- Implement real authorization adapters backed by trusted persisted membership/assignment data; prohibit test-only allow-all adapters in production registration.
- Add negative tests for cross-customer order access, merchant/store mismatch, unassigned-driver commands, role escalation, replay after permission revocation, and unauthenticated requests.

**Exit evidence:** accepted API/security gate, production authorization implementation, negative tests, and CI green.

### Gate 3 — Implement a thin, vertical HTTP slice
- Add the ASP.NET Core host and dependency-injection composition root.
- Expose only the approved Create Order and customer order-read contracts first.
- Map typed application failures to stable Problem Details responses; never infer status from exception message text.
- Add API integration tests against PostgreSQL for success, validation, conflict, idempotency replay, ownership boundaries, and trace IDs.
- Document local run/configuration instructions without committing secrets.

**Exit evidence:** runnable API, OpenAPI contract, integration tests, and green CI.

### Gate 4 — Build the first customer mobile slice
- Close the mobile-stack design gate explicitly.
- Build a small Android/iOS vertical slice for app startup, language/RTL switching, location selection, category discovery, and API error/loading/empty states.
- Do not use mocked screens as evidence of backend integration.
- Verify builds and basic navigation on both target platforms.

**Exit evidence:** accepted stack decision, reproducible platform builds, accessibility/RTL checks, and one tested API-backed customer flow.

### Gate 5 — Pilot readiness
- Complete merchant operations, order lifecycle, delivery lifecycle, and operational intervention policies.
- Add structured logs, correlation IDs, health checks, metrics, alerting, secret management, migration/rollback strategy, backup and restore runbook, and deployment smoke tests.
- Run security, load, failure-recovery, and end-to-end tests against the intended pilot environment.
- Define pilot limits, support ownership, incident response, and measurable go/no-go criteria.

**Exit evidence:** signed-off pilot checklist with evidence from the target environment.

## Current owner decision needed

The mobile stack proposal in PR #16 must be explicitly accepted or rejected before it is treated as an architecture decision. No other gate in this document should be marked closed merely because its documentation exists.
