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

## Required Decision Record
For every significant decision record:
- context
- problem
- options
- trade-offs
- decision
- consequences
- verification
