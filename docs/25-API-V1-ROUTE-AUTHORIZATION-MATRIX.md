# API v1 Route Authorization Matrix — Working Draft

Status: **DRAFT — CONTRACT CLOSURE EVIDENCE — NOT OWNER APPROVAL**

## Purpose and boundary

Translate the approved API/security direction in `docs/07-API-SECURITY-DESIGN-GATE.md` into a route-by-route authorization review artifact. This document does not select an authentication provider, approve the initial route scope, or authorize HTTP/controller implementation.

The server must derive actor identity from a verified authentication context. A client-supplied CustomerId, MerchantId, DriverId, StoreId, or role value is never proof of identity or resource access.

## Route-by-route matrix

| Route | Intended actor (draft) | Required resource authorization | Negative case that must be tested | Contract still to close |
|---|---|---|---|---|
| `POST /api/v1/orders` | Authenticated customer | Create the order for the customer bound to the authenticated principal; never accept an authoritative customer ID from the request | Customer A cannot create an order on behalf of Customer B by changing request fields | Exact request DTO, cart selection/ownership contract, success response |
| `GET /api/v1/orders/{orderId}` | Authenticated customer | Order belongs to authenticated customer | Missing order and another customer's order return indistinguishable public 404 responses | DTO composition, optional delivery/history fields, not-found concealment test |
| `POST /api/v1/orders/{orderId}/cancel` | Authenticated customer | Order belongs to authenticated customer; application enforces lifecycle/cancellation rules | Another customer cannot cancel the order; post-pickup cancellation is rejected by the approved lifecycle rule | Exact cancellation body/reason, response semantics, public error mapping |
| `POST /api/v1/orders/{orderId}/ready-for-pickup` | Authenticated merchant actor | Order belongs to a store the authenticated merchant is authorized to operate | Merchant cannot mark another merchant's order ready | Merchant-to-store membership model, DTO, success response |
| `POST /api/v1/deliveries/{deliveryId}/assignment` | Authorized dispatch/operations actor (proposed; owner decision required) | Actor has explicit permission to assign a driver; selected driver meets eligibility rules | Customer, unrelated merchant, or unprivileged driver cannot assign delivery | Owner must approve actor/permission policy and whether this route belongs in the first HTTP slice |
| `POST /api/v1/deliveries/{deliveryId}/pickup` | Authenticated assigned driver | Delivery is assigned to the authenticated driver | Unassigned or different driver cannot pick up | Request fields, exact state-conflict mapping |
| `POST /api/v1/deliveries/{deliveryId}/start` | Authenticated assigned driver | Delivery is assigned to the authenticated driver and pickup/state prerequisites are met | Different driver cannot start; invalid lifecycle state is rejected | Request DTO if any, response semantics, exact state-conflict mapping |
| `POST /api/v1/deliveries/{deliveryId}/complete` | Authenticated assigned driver | Delivery is assigned to the authenticated driver and completion prerequisites are met | Different driver cannot complete a delivery | Proof-of-delivery requirements are not decided; response semantics |
| `POST /api/v1/deliveries/{deliveryId}/fail` | Authenticated assigned driver, with operational override separately authorized if approved | Delivery is assigned to the driver unless a distinct operational permission permits intervention | Different driver cannot fail delivery; caller cannot submit arbitrary actor identity | Allowed failure codes, reason constraints, operational override policy |

These actor labels are proposed interpretations of the existing design gate, not a claim that the permission model has been approved. In particular, dispatch/assignment authority and operational override remain explicit decision items.

## Cross-cutting requirements for every included route

- Authenticate before executing a protected application operation.
- Perform resource-level authorization; a role claim alone does not grant access to a specific Order, Store, or Delivery.
- Require and validate the proposed `Idempotency-Key` contract for state-changing commands before invoking their handlers. Preserve the same key for retries of the same logical operation.
- Map known stable application error codes to the reviewed HTTP contract; never classify failures by matching exception-message text.
- Return safe RFC 9457 Problem Details with a trace identifier where available. Do not expose stack traces, database/provider messages, credentials, tokens, or raw idempotency keys.
- Ensure cancellation and transaction failures preserve atomicity and idempotency guarantees.
- Add tests for unauthenticated access, wrong actor, wrong resource ownership/assignment, invalid state, idempotent replay, fingerprint conflict, and safe unexpected-error handling for every route in the approved slice.

## Decisions and evidence still required

1. **Initial HTTP slice:** owner must choose customer-order routes only, or explicitly approve a broader initial route set. The matrix does not make this decision.
2. **Identity integration:** owner must select the authentication integration approach after reviewing `docs/23-AUTHENTICATION-OPTIONS-DESIGN-GATE.md`. Identity provider choice does not replace application authorization.
3. **Actor/permission model:** approve merchant-to-store relationships, driver assignment authority, dispatch permissions, and any administrative override.
4. **DTO and HTTP contract:** specify request/response schemas, status/body/headers, validation limits, and stable error mappings for every included route.
5. **Verification:** add route-level negative authorization tests and HTTP adapter contract tests before marking this gate PASS.

## Exit criteria

This worksheet can be marked complete only when each route included in the owner-approved slice has:
- an approved actor source and resource authorization rule;
- a concrete request/response contract and success status;
- a complete stable application-error-to-HTTP mapping;
- negative authorization and concealment tests where applicable;
- idempotency/replay and transaction-failure evidence for state-changing commands;
- OpenAPI consistency evidence.

Until these criteria and the relevant owner decisions are recorded, HTTP implementation remains blocked. Phase B product implementation also remains blocked by the unresolved pilot wedge.
