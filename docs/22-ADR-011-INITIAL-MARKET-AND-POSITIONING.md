# ADR-011 — Initial Market Context and Product Positioning

Status: **ACCEPTED — OWNER CONFIRMED**

## Context

The platform needs an explicit initial market context to guide product language, currency, geographic discovery, and merchant/customer experience. This decision records only the direction explicitly confirmed by the project owner. It does not decide the pilot's merchant cohort, exact service zones, provider choices, legal compliance, or operating economics.

## Decision

Record the following owner-confirmed product context:

- **Country:** Egypt.
- **Initial geographic focus:** Dekernes, Dakahlia.
- **Product languages:** Arabic and English.
- **Currency:** Egyptian pound (EGP).
- **Positioning:** local product discovery across multiple local stores, with a straightforward merchant experience for updating products and their availability.
- **Long-term category ambition:** broad coverage across merchant categories.
- **Delivery direction:** hybrid delivery.

## Explicit boundaries and unresolved details

This ADR does **not** decide:
- Exact delivery/service zones, radius rules, or geographic rollout boundaries.
- Which merchant categories participate in the first pilot.
- Initial pilot merchant count/cohort size.
- The hybrid operating split, including driver engagement/payment, dispatch responsibilities, cash collection, reconciliation, and settlement.
- Commission/fee types, payer, values, formulas, discounts, or settlement.
- Authentication, notifications, hosting, image storage, maps/geocoding, mobile stack, or other vendors.
- Applicable legal obligations or whether the platform is legally compliant.
- Numeric pilot success thresholds.

The long-term ambition for broad category coverage must not be interpreted as a commitment to launch the pilot with every category. Phase B remains blocked until the owner approves a narrow pilot wedge and its blocking decisions.

## Alternatives considered

1. **Leave all initial market context open.** Rejected because the owner has explicitly supplied these product inputs.
2. **Treat the broad category ambition and hybrid direction as a complete pilot/operating plan.** Rejected because it would invent unresolved operating details and pilot scope.
3. **Record only confirmed context and keep implementation-dependent details OPEN.** Accepted because it preserves traceability without overcommitting.

## Consequences

- Product documentation may use Egypt, Dekernes/Dakahlia, Arabic/English, EGP, the stated positioning, and hybrid delivery as the current direction.
- Detailed geography, category wedge, pilot scale, fees, operations, providers, legal review, and success thresholds remain OPEN.
- No implementation or external provider selection is authorized solely by this ADR.
- This ADR does not change the provisional status of ADR-005 (Cash on Delivery initially).

## Verification

The decision is grounded in the project owner's explicit answers during project planning. The relevant documentation is updated in focused changes; the pilot wedge and unresolved items remain in `docs/14-OPEN-QUESTIONS.md`.

## ADR numbering

Existing ADR-001 through ADR-010 retain their identities. Newly introduced decision records start at ADR-011; existing decisions and cross-references must not be blindly renumbered.
