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

## 17. Identity and Account Lifecycle

- Use ASP.NET Core Identity for account management and OpenIddict for OAuth 2.0 / OpenID Connect protocol endpoints and token issuance. Identity alone is not treated as a complete authorization server.
- The target direction is .NET 10 LTS, but a framework/package upgrade requires a separate compatibility review and a green full-solution verification; no partial or blind upgrade.
- The first authenticated API slice is limited to the safe health endpoint and customer commerce-order operations. Merchant, driver, admin, payment, public catalog, and broader service-request APIs are outside this slice unless separately approved.
- Use one login/security account per person for the first version. A person may act as both customer and merchant without creating a second login. Roles describe broad capabilities and never replace resource-ownership checks.
- A Customer is the buyer/person who owns customer-facing orders; a customer profile is commerce-side profile data; an Identity account is authentication/security state. These concepts must remain separate and explicitly linked by a trusted server-side workflow.
- A Merchant is a business actor and is distinct from a Store; a Merchant may own multiple Stores. Merchant access must be scoped to an active, verified business/store membership. A MERCHANT role alone does not authorize access to every business or Store.
- Creating a login does not automatically create or claim a customer profile, merchant business, Store, or historical order. Contact verification alone is not proof of ownership of existing commerce records.
- Disabling a login account suspends platform access across roles, including new authentication and refresh operations; the chosen revocation/validation mechanism must prevent continued use of existing sessions promptly.
- Suspending a Merchant or Store is separate from suspending the person's login. It blocks the affected business operations without automatically disabling unrelated customer capabilities.
- Disablement is not deletion or privacy erasure. Do not cascade-delete orders, order history, payment/refund records, or other operational/financial history when an account/profile is disabled or removed. Retention, anonymization and erasure rules require a separately documented policy.
- Never expose legacy orders without verified customer ownership, and never accept a client-supplied CustomerId as proof of identity. Missing, inactive, ambiguous or unverified mappings fail closed.
- The detailed security profile, provisioning/verification process, Identity/OpenIddict persistence and migration ownership, recovery/abuse controls, token lifetimes/revocation, and retention/anonymization rules remain subject to the authentication design gate. See [Customer Authentication and First API Slice — Design Gate](32-CUSTOMER-AUTH-AND-FIRST-API-SLICE-DESIGN-GATE.md). This section records product/architecture constraints; it does not by itself authorize implementation.

## 18. Decision Records

- Preserve existing ADR numbering and content. New Architecture Decision Records start at ADR-011.
- Owner-approved product decisions must be reflected in this Constitution and the relevant Design Gate before implementation.
- Where this Constitution records a product invariant but implementation details remain unresolved, keep those details OPEN; do not infer approval from the high-level decision.
