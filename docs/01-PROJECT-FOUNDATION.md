# Project Foundation

## Problem

Local merchants often lack a simple, maintainable digital storefront, while customers have fragmented ways to discover products that are actually available at local stores.

## Product

The platform provides:
- Merchant digital storefronts and product/availability management.
- Local product discovery across multiple stores.
- Customer ordering.
- Merchant order operations.
- Driver delivery operations.
- Platform administration.

The merchant experience should make it straightforward to update products and their availability. Customer discovery should help people find local products across participating stores, while the initial cart remains single-store under the accepted MVP constraint.

## Actors

1. Customer
2. Merchant
3. Driver
4. Administrator

## Owner-confirmed initial context

- **Country:** Egypt.
- **Initial geographic focus:** Dekernes, Dakahlia. The exact service zones, delivery radius, and rollout boundaries remain OPEN.
- **Languages:** Arabic and English.
- **Currency:** EGP.
- **Positioning:** local product discovery across multiple local stores, paired with a simple merchant experience for maintaining products and availability.
- **Delivery direction:** hybrid delivery is the intended direction. The operating split is not yet decided, including driver engagement/payment responsibility, who collects cash on delivery, settlement timing, and responsibility allocation.
- **Long-term category ambition:** broad coverage across merchant types. This is a long-term product ambition, not a decision to include every category in the initial pilot.

These are owner-provided product inputs. They do not decide vendor selection, legal compliance, pricing, pilot scale, or operating procedures.

## Positioning

Local Store Infrastructure + Local Product Discovery + Local Delivery.

The platform should not depend on competing only on delivery speed. The product's distinguishing value includes helping customers discover products from local stores and helping merchants maintain an accurate, usable digital catalog.

## MVP In Scope — Current Baseline, Subject to Required Gates

- Identity and roles.
- Merchants and stores.
- Store catalog.
- Product availability.
- Customer addresses.
- Single-store cart.
- Checkout.
- Cash-on-delivery flow, subject to ADR-005 remaining PROVISIONAL until its scope/status is explicitly resolved.
- Merchant order inbox.
- Order lifecycle.
- Delivery lifecycle.
- Basic administration.
- Audit/history.
- Notification baseline, with channels/provider still OPEN.

## MVP Out of Scope — Unless Explicitly Re-decided

- AI recommendations.
- Loyalty.
- Wallet.
- Subscriptions.
- Multi-store cart.
- Dynamic pricing.
- Advanced inventory/ERP.
- Advanced advertising.
- Complex route optimization.
- Online payment before its Design Gate.

## Pilot Decisions Still Open — Blocking Phase B

- **Pilot wedge:** first merchant categories and the small initial cohort.
- Pilot merchant count.
- Exact operating zones and delivery-radius rules.
- Merchant onboarding model.
- Hybrid delivery operating model and responsibilities.
- Configurable commission/fee design, including fee types, payer, values, calculation rules, collection, and settlement.
- Merchant and driver operating expectations/SLA.

The broad category ambition must not be used as a substitute for an explicit, narrow pilot decision. Do not begin Phase B implementation until the pilot wedge and the decisions that block it are approved.

## Success Measures

Candidate measures to be reviewed and approved by the owner before they become pilot acceptance thresholds:
- Orders per day.
- Average order value.
- Delivery time.
- Cancellation rate.
- Merchant acceptance rate.
- Driver utilization.
- Repeat customers and customer retention.
- Merchant retention.
- Contribution per order.

No numeric target or success threshold is approved by this document.
