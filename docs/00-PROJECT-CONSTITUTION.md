# Local Commerce Platform — Project Constitution

This document is the project's engineering constitution and highest-level working agreement.

## 1. Source of Truth
- GitHub is the source of truth for code, documentation, decisions, issues, and delivery status.
- A decision is not committed until documented.
- Code is not complete until verification evidence exists.
- Documentation is part of Definition of Done.

## 2. Engineering Workflow
Understand -> Map -> Design -> Trade-offs -> Decide -> TDD/Tests -> Implement -> Verify -> Document

No blind coding.

Major features and architecture changes require a Design Gate covering problem, scope, non-goals, affected domains, constraints, options, trade-offs, risks, decision, acceptance criteria, and verification.

## 3. Decision Governance
PROPOSED -> ANALYSIS -> TRADE-OFFS -> DECISION -> DOCUMENTED -> IMPLEMENTED -> VERIFIED

Assumptions are explicitly marked. Unknowns remain OPEN. Unverified claims remain NOT PROVEN.

Any change that conflicts with this constitution must either comply or explicitly amend it with rationale and impact.

## 4. Architecture
- Start with a Modular Monolith.
- Keep domain/application/infrastructure boundaries clear.
- Do not introduce microservices without a demonstrated need.
- Do not add Redis, Kafka/RabbitMQ, Elasticsearch/OpenSearch, Kubernetes, service discovery, or similar infrastructure for hypothetical scale.
- Infrastructure additions require a real requirement, evidence of the current limitation, alternatives, and a documented decision.

## 5. Domain Rules
- Merchant and Store are different concepts.
- A Merchant may own multiple Stores.
- Order, Payment, and Delivery are separate concerns.
- Their statuses must not be collapsed into one field.
- Orders snapshot purchased names, variants, prices, quantities, discounts, and relevant purchase-time data.
- Cart is initially single-store.
- MVP inventory is availability-oriented, not a full ERP inventory system.
- Delivery is modeled independently from Order.
- Sensitive operations must be idempotent.
- Business rules do not belong in controllers.

## 6. API Rules
- Version public APIs.
- API DTOs are separate from domain entities.
- Controllers orchestrate; they do not contain business rules.
- Prefer intention-revealing commands such as accept, prepare, ready, pickup, and deliver.
- Standardize API errors and include a correlation/trace identifier.
- Enforce authorization at resource level.

## 7. Security
- HTTPS in deployed environments.
- Secure authentication and authorization.
- Resource ownership checks.
- Rate limiting where appropriate.
- Audit logging for sensitive operations.
- Secrets never committed.
- Never store passwords or card data improperly.
- Validate external input.

## 8. Reliability
For order-critical operations define transaction boundaries, failure modes, retry behavior, idempotency, audit/history, and recovery behavior.

A workaround that hides the root cause is not a fix.

## 9. Testing
Testing is layered:
- Domain
- Application/use-case
- API/contract
- Integration
- End-to-end smoke tests for critical flows

Preferred cycle: RED -> GREEN -> REFACTOR -> VERIFY.

A green suite alone does not prove business correctness; acceptance criteria and integration evidence are required.

## 10. Observability
Minimum baseline:
- structured logs
- correlation IDs
- order lifecycle history
- audit events for sensitive actions
- standardized error codes
- enough diagnostic context to investigate failures

## 11. Scope Discipline
MVP focuses on:
Customer -> Store -> Product -> Cart -> Order -> Merchant confirmation -> Preparation -> Ready -> Delivery -> Delivered

AI recommendations, loyalty, wallet, subscriptions, multi-store cart, dynamic pricing, advanced inventory, advanced advertising, and complex route optimization are not MVP requirements unless explicitly re-decided.

## 12. Root-Cause Rule
When a failure appears:
1. Reproduce it.
2. Isolate the boundary.
3. Identify the root cause.
4. Fix the root cause.
5. Add a regression test.
6. Verify the complete affected flow.
7. Document the lesson if it can recur.

Do not repeatedly patch symptoms.

## 13. Branch and Change Discipline
- Keep the default branch releasable.
- Use focused branches for meaningful changes.
- Keep commits coherent and purpose-driven.
- Remove stale branches and duplicate implementations.
- Before merge, verify tests, documentation, migration impact, and acceptance criteria.
- Do not mix unrelated changes.

## 14. Definition of Done
A feature is Done only when applicable business rules, architecture review, tests, authorization, failure handling, API contracts, migrations, observability, documentation, acceptance criteria, regression verification, and blockers are addressed.

## 15. Engineering Maturity
Optimize for correctness, clarity, traceability, and sustainable delivery rather than raw coding speed.

Every milestone should leave the repository more understandable and more verifiable than before.

## 16. Constitution Amendments
Any amendment records:
- what changes
- why
- alternatives
- risks
- impact/migration
- verification plan
