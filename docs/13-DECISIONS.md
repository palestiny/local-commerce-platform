# Decision Log

| ID | Decision | Status | Rationale |
|---|---|---|---|
| ADR-001 | Modular Monolith | ACCEPTED | Low operational complexity with clear boundaries. |
| ADR-002 | GitHub as source of truth | ACCEPTED | Traceability for code, decisions, and documentation. |
| ADR-003 | Single-store cart for MVP | ACCEPTED | Reduces pricing, fulfillment, and delivery complexity. |
| ADR-004 | Separate Order, Payment, and Delivery state | ACCEPTED | Prevents unrelated lifecycle concerns from becoming coupled. |
| ADR-005 | Cash on Delivery initially | PROVISIONAL | Simplifies pilot; online payments remain a later Design Gate. |
| ADR-006 | Availability-based MVP inventory | ACCEPTED | Avoids building ERP before validating commerce transaction. |
| ADR-007 | Strict separation of Order and Delivery state authority | ACCEPTED | Order owns commercial state; Delivery owns fulfillment state, avoiding duplicate mutable sources of truth. |
| ADR-008 | EF Core + PostgreSQL for M0 persistence | ACCEPTED | Reliable relational transactions, constraints, migrations, and concurrency verification without unnecessary infrastructure complexity. |
| ADR-009 | PostgreSQL row-level locking for Delivery command concurrency | ACCEPTED | Short Delivery state-transition transactions are serialized at the database row boundary; optimistic concurrency remains deferred until measured need. |
| ADR-010 | Generalized command idempotency persistence | ACCEPTED | Cross-cutting idempotency is resource-neutral and shared across Order, Delivery, Payment, and future commands. |
| ADR-011 | Initial market context and product positioning | ACCEPTED | Records the owner's explicit country, geographic focus, language, currency, positioning, broad long-term category ambition, and hybrid-delivery direction without inventing pilot scope or operating details. |

## ADR numbering policy

- ADR-001 through ADR-010 retain their existing identities and historical references.
- Newly introduced decision records start at ADR-011.
- Do not blindly renumber existing files or references.
- Before assigning a number, inspect the current Decision Log and ADR documents to avoid duplicate decisions.
- The detailed record for ADR-011 is [ADR-011 — Initial Market Context and Product Positioning](22-ADR-011-INITIAL-MARKET-AND-POSITIONING.md).

## Required Decision Record

For every significant decision record:
- context
- problem
- options
- trade-offs
- decision
- consequences
- verification

## Status semantics

- **PROPOSED:** recommendation awaiting owner decision.
- **PROVISIONAL:** limited/temporary direction that is not fully accepted; do not treat as final.
- **ACCEPTED:** explicitly decided and recorded.
- **IMPLEMENTED:** code/configuration implements the decision; this does not itself prove correctness.
- **VERIFIED:** required evidence demonstrates the defined acceptance criteria within its stated scope.

A green CI run alone does not automatically change a decision's status or prove broader pilot readiness.
