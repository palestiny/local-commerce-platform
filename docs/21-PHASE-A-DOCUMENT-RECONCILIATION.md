# Phase A — Existing Documentation Reconciliation

Status: **PROPOSED RECONCILIATION MAP — NOT A DESIGN-GATE PASS**

## Purpose

Map the operating contract's Appendix A draft-document intent against the documentation and implementation evidence already present on `feature/foundation-domain-architecture`. This file is an inventory and migration plan only. It does not replace, rename, or silently approve existing documents.

## Governance decisions confirmed by the owner

1. Preserve the existing ADR-001 through ADR-010 identities and their recorded decisions.
2. Assign newly introduced decision records starting at ADR-011.
3. Reconcile existing documentation with the Appendix A drafts before editing or replacing content.
4. Keep all owner-dependent decisions PROPOSED/OPEN until explicitly approved.
5. Do not start Phase B implementation until the pilot wedge and all Phase B blocking decisions are closed.

These points authorize document reconciliation, not automatic acceptance of every proposal in Appendix A.

## Repository baseline

The reviewed feature branch contains a .NET solution, Domain/Application/Infrastructure code, tests, and documents beyond the clean-documentation-only baseline described in the supplied operating contract. The latest observed CI run for commit `31a76f46cb4349f00068faebf1e51410c234d47d` completed successfully. That run is evidence for that commit and workflow only; it is not proof of API security or pilot readiness.

Existing accepted decisions include:
- ADR-001 Modular Monolith
- ADR-002 GitHub as source of truth
- ADR-003 Single-store cart for MVP
- ADR-004 Separate Order, Payment, and Delivery state
- ADR-006 Availability-based MVP inventory
- ADR-007 Strict separation of Order and Delivery state authority
- ADR-008 EF Core + PostgreSQL for M0 persistence
- ADR-009 PostgreSQL row-level locking for Delivery command concurrency
- ADR-010 Generalized command idempotency persistence

ADR-005 (Cash on Delivery initially) is recorded as PROVISIONAL, not fully accepted. Do not silently upgrade its status.

## Reconciliation map

| Appendix A intent / target | Existing evidence on reviewed branch | Safe treatment |
|---|---|---|
| `docs/02` domain architecture / domain design gate | `docs/02-DOMAIN-ARCHITECTURE-DESIGN-GATE.md`; `docs/05-MODULE-BOUNDARIES.md`; existing Domain/Application/Infrastructure code | Compare scope, invariants, and boundaries. Keep the existing gate; add only explicit gaps after review. |
| `docs/03` order lifecycle | `docs/03-ORDER-LIFECYCLE.md`; `docs/09-ADR-007-ORDER-DELIVERY-STATE-AUTHORITY.md`; `docs/16-M1-DELIVERY-DESIGN-GATE.md` | Reconcile lifecycle vocabulary and authority. ADR-007 remains the existing accepted decision. |
| `docs/04` testing strategy | `docs/04-TESTING-STRATEGY.md`; CI evidence linked from the existing M1 documents | Preserve the testing strategy and make evidence boundaries explicit; do not treat a green suite as universal correctness proof. |
| `docs/05` module boundaries | `docs/05-MODULE-BOUNDARIES.md` plus the current solution/project structure | Review for contract alignment; do not introduce a second competing module-boundary document without a demonstrated gap. |
| `docs/06` persistence / reliability gate | `docs/06-PERSISTENCE-RELIABILITY-DESIGN-GATE.md`; ADR-008, ADR-009, ADR-010 | Preserve accepted persistence and concurrency decisions; identify only unresolved reliability gaps. |
| `docs/07` API / security gate | `docs/07-API-SECURITY-DESIGN-GATE.md`; `docs/19-API-APPLICATION-ERROR-TAXONOMY-PROPOSAL.md` | Keep HTTP implementation blocked until the required API, auth, authorization, idempotency, abuse-control, and error-mapping decisions/tests are closed. |
| `docs/08` foundation review checklist | `docs/08-FOUNDATION-REVIEW-CHECKLIST.md` | Reuse and update after a documented gap review; no wholesale replacement. |
| `docs/09` proposed ADR-007 | `docs/09-ADR-007-ORDER-DELIVERY-STATE-AUTHORITY.md` is already ADR-007 and ACCEPTED | Do not create a second ADR-007. Any new decision record starts at ADR-011. If Appendix A's draft expresses a different decision, record the conflict explicitly rather than overwriting this accepted ADR. |
| `docs/10` M0 TDD RED / Create Order | `docs/10-M0-TDD-RED-CREATE-ORDER.md` and the existing Create Order implementation/tests | Treat as existing implementation history/evidence. Verify current truth before changing status or wording. |
| `docs/11` M0 Create Order application gate | `docs/11-M0-CREATE-ORDER-APPLICATION-DESIGN-GATE.md` and current application/test implementation | Compare acceptance criteria with current evidence; update only identified gaps and preserve the audit trail. |

The table maps by intent and known file inventory; it is not a claim that every paragraph in Appendix A has been exhaustively diffed against every existing document.

## Owner-confirmed product context

- Country: Egypt.
- Initial geography: Dekernes, Dakahlia; detailed delivery zones and radius rules remain OPEN.
- Languages: Arabic and English.
- Currency: EGP.
- Positioning: local product discovery across multiple stores, with a simple merchant experience for updating products and availability.
- Delivery direction: hybrid; operational responsibilities, collection of COD, settlement, and payer split remain OPEN.
- Long-term category ambition is broad. It does **not** decide the initial pilot categories.
- Pilot merchant count and the narrow initial wedge remain OPEN.
- Fee/commission rules should be configurable; fee types, payer, values, calculation rules, and settlement are OPEN.

## Decisions that remain OPEN / blocked

- Pilot wedge: first categories, a small initial merchant cohort, and the precise operating zones.
- Authentication provider and lifecycle; resource-level role/permission matrix.
- Notification channels/provider.
- Hosting/deployment target and budget constraints.
- Object/image storage provider.
- Maps/geocoding provider.
- License choice and legal review/counsel.
- Merchant onboarding, delivery-radius rules, merchant/driver SLAs, cancellation/refund, support and dispute workflows.
- Configurable commercial-fee model details and hybrid delivery operating split.
- Mobile/web strategy and mobile stack.

No provider, price, legal conclusion, success threshold, or scale target is inferred by this map.

## ADR numbering and references

- Existing ADR-001 through ADR-010 IDs are immutable historical references for the decisions already recorded.
- New decision records start at ADR-011.
- Before adding a new ADR, search the current Decision Log and existing ADR files to avoid duplicate decisions.
- Update links/references only in the same focused change that has been reviewed for that document; do not perform a blind global renumber.
- ADR-011 is assigned to **Initial Market Context and Product Positioning** in `docs/22-ADR-011-INITIAL-MARKET-AND-POSITIONING.md`. Further new decisions continue at ADR-012 only when a distinct decision record is ready.

## Next document changes

Use one focused PR per existing document. Proposed order:

1. Update `docs/01-PROJECT-FOUNDATION.md` with only the owner-confirmed product context above, while keeping pilot categories/count and operating details OPEN.
2. Update `docs/14-OPEN-QUESTIONS.md` to remove questions already answered by the owner and retain unresolved items.
3. Update `docs/13-DECISIONS.md` with owner-confirmed decisions and status labels, preserving ADR-001–010 and ADR-005's PROVISIONAL status.
4. Review remaining existing gates against the reconciliation map and submit focused changes only where a concrete gap is identified.

Each change remains subject to the project's Design Gate, verification, and review requirements. PRs remain unmerged pending the owner's explicit merge approval. Phase B implementation remains blocked until the pilot wedge and its blocking decisions are approved.

## Verification and limitations

- Reviewed document inventory and contents on `feature/foundation-domain-architecture`.
- Latest observed CI evidence: run `37867776559` for commit `31a76f46cb4349f00068faebf1e51410c234d47d`, conclusion `success`.
- No code or runtime behavior is changed by this reconciliation map.
- This document does not pass Phase A or any later STOP GATE.
